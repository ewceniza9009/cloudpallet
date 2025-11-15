using MediatR;
using WMS.Application.Abstractions.Integrations;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums; // <-- ADDED
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.Inventory.Commands;

public record TransferItemsToNewPalletCommand(
    Guid SourceInventoryId,
    decimal QuantityToMove,
    Guid NewPalletTypeId,
    decimal? WeighedWeight) : IRequest<Guid>
{
    public Guid UserId { get; init; }
}

public class TransferItemsToNewPalletCommandHandler(
    IMaterialInventoryRepository inventoryRepository,
    IReceivingTransactionRepository receivingRepository,
    IPalletTypeRepository palletTypeRepository,
    IItemTransferTransactionRepository itemTransferRepository,
    IVASTransactionRepository vasRepository, // <-- ADDED VAS Repo
    IBarcodeGenerationService barcodeService,
    IMaterialRepository materialRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<TransferItemsToNewPalletCommand, Guid>
{
    public async Task<Guid> Handle(TransferItemsToNewPalletCommand request, CancellationToken cancellationToken)
    {
        // 1. Get and Validate Source Inventory
        var sourceInventory = await inventoryRepository.GetByIdWithTrackingAsync(request.SourceInventoryId, cancellationToken)
           ?? throw new KeyNotFoundException($"Source inventory with ID {request.SourceInventoryId} not found."); // Corrected lookup by ID

        if (sourceInventory.Quantity < request.QuantityToMove)
        {
            throw new InvalidOperationException("Insufficient quantity in source inventory to perform transfer.");
        }

        var palletType = await palletTypeRepository.GetByIdAsync(request.NewPalletTypeId, cancellationToken)
           ?? throw new KeyNotFoundException($"Pallet Type with ID {request.NewPalletTypeId} not found.");

        // 2. Calculate Weight
        decimal weightToMove;
        if (request.WeighedWeight.HasValue)
        {
            // Use the manually provided weight (net weight of the transferred portion)
            weightToMove = request.WeighedWeight.Value;
        }
        else
        {
            // Calculate proportional weight based on the material's base unit weight
            var material = await materialRepository.GetByIdAsync(sourceInventory.MaterialId, cancellationToken)
               ?? throw new KeyNotFoundException("Material not found.");
            weightToMove = request.QuantityToMove * material.BaseWeight;
        }

        // 3. Create the New Pallet (for the Credit/Transfer-In)
        var newPallet = Pallet.Create(
            sourceInventory.Pallet.ReceivingId, // Inherit receiving session for context
            request.NewPalletTypeId,
            (sourceInventory.Pallet.Receiving.TotalPallets + 1).ToString(), // New sequential number
            palletType.TareWeight,
            sourceInventory.AccountId);

        var palletBarcode = barcodeService.GenerateSSCCBarcode(newPallet.Id);
        newPallet.SetBarcode(palletBarcode);
        await receivingRepository.AddPalletAsync(newPallet, cancellationToken);
        sourceInventory.Pallet.Receiving.IncrementPalletCount();

        // 4. Create New Pallet Line and Inventory (Credit)
        var newPalletLine = Domain.Entities.Transaction.PalletLine.Create(
           newPallet.Id,
           sourceInventory.MaterialId,
           request.QuantityToMove,
           weightToMove,
           sourceInventory.BatchNumber,
           DateTime.UtcNow, // Assuming DateOfManufacture should be now for the new line
           sourceInventory.ExpiryDate,
           sourceInventory.AccountId
       );
        await receivingRepository.AddPalletLineAsync(newPalletLine, cancellationToken);

        // Note: Since the PalletLine ID is now available, we can assign its LPN/Barcode
        var itemLpn = barcodeService.GenerateSSCCBarcode(newPalletLine.Id);
        newPalletLine.SetBarcode(itemLpn);
        // Ensure the PalletLine status is marked as processed/complete
        newPalletLine.Update(request.QuantityToMove, weightToMove, sourceInventory.BatchNumber, DateTime.UtcNow, sourceInventory.ExpiryDate);

        var newInventory = MaterialInventory.Create(
           sourceInventory.MaterialId,
           sourceInventory.LocationId, // Remains in the original location for now
           newPallet.Id,
           newPalletLine.Id,
           request.QuantityToMove,
           sourceInventory.BatchNumber,
           Weight.Create(weightToMove, "KG"),
           sourceInventory.ExpiryDate,
           sourceInventory.AccountId,
           itemLpn);
        await inventoryRepository.AddAsync(newInventory, cancellationToken);

        // 5. Adjust Source Inventory (Debit)
        sourceInventory.AdjustForWeighedPick(request.QuantityToMove, weightToMove);

        // 6. Create Audit Trail - ItemTransferTransaction
        // The transaction needs the ID of the old inventory record (sourceInventory.Id)
        var transferTransaction = ItemTransferTransaction.Create(sourceInventory.Id, newPallet.Id, request.QuantityToMove, weightToMove, request.UserId);
        await itemTransferRepository.AddAsync(transferTransaction, cancellationToken);

        // --- START: NEW STEP - Create VAS Transaction for Billing ---
        var sourceMaterial = await materialRepository.GetByIdAsync(sourceInventory.MaterialId, cancellationToken)
           ?? throw new KeyNotFoundException("Source material not found."); // Fetch for description

        var vasDescription = $"Split {request.QuantityToMove} units of '{sourceMaterial.Name}' from Pallet {sourceInventory.Pallet.Barcode} to new Pallet {newPallet.Barcode}.";

        var vasTransaction = VASTransaction.Create(
            sourceInventory.AccountId,
            sourceInventory.PalletId, // Associated with the source pallet
            ServiceType.Split, // Use the new enum value
            request.UserId,
            vasDescription);

        // Add the consumed item as input
        vasTransaction.AddInputLine(sourceInventory.MaterialId, request.QuantityToMove, weightToMove);
        // Add the created item as output (though quantity/weight are same, conceptually it's output)
        vasTransaction.AddOutputLine(sourceInventory.MaterialId, request.QuantityToMove, weightToMove);

        vasTransaction.Complete();
        await vasRepository.AddAsync(vasTransaction, cancellationToken);
        // --- END: NEW STEP ---

        // 7. Commit all changes
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return newPallet.Id;
    }
}