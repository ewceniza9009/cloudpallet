// ---- File: src/Core/WMS.Application/Features/Inventory/Commands/RecordLabelingCommand.cs ----

using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities; // Needed for VASTransaction
using WMS.Domain.Entities.Transaction; // Needed for Pallet
using WMS.Domain.Enums;

namespace WMS.Application.Features.Inventory.Commands;

public record RecordLabelingCommand(
    Guid TargetId, // ID of the Pallet or MaterialInventory being labeled
    TargetType TargetType,
    ComplianceLabelType LabelType, // The type of label applied (e.g., Export, Allergen)
    decimal QuantityLabeled // 1 if TargetType is Pallet, or specific item quantity if InventoryItem
    ) : IRequest
{
    public Guid UserId { get; init; } // Automatically set by controller or context
}

public class RecordLabelingCommandHandler(
    IMaterialInventoryRepository inventoryRepository,
    IVASTransactionRepository vasRepository,
    IReceivingTransactionRepository receivingRepository, // To get pallet details if TargetType is Pallet
    IUnitOfWork unitOfWork)
    : IRequestHandler<RecordLabelingCommand>
{
    public async Task Handle(RecordLabelingCommand request, CancellationToken cancellationToken)
    {
        Guid accountId;
        Guid? palletId = null; // Store pallet ID for VAS transaction
        string description;
        List<MaterialInventory> itemsToUpdate = new(); // List to hold inventory items whose flags need updating

        if (request.TargetType == TargetType.Pallet)
        {
            // Find the pallet and its associated inventory items
            var pallet = await receivingRepository.GetPalletWithLinesByIdAsync(request.TargetId, cancellationToken)
                ?? throw new KeyNotFoundException($"Pallet with ID {request.TargetId} not found.");

            palletId = pallet.Id;
            accountId = pallet.AccountId; // Get account from the pallet

            var inventoryItemsOnPallet = await inventoryRepository.GetByPalletIdAsync(pallet.Id, cancellationToken);
            itemsToUpdate.AddRange(inventoryItemsOnPallet); // Add all items on the pallet

            description = $"Applied {request.LabelType} labeling to Pallet {pallet.Barcode} ({itemsToUpdate.Count} total items).";
        }
        else // TargetType.InventoryItem
        {
            // Find the specific inventory item
            var inventoryItem = await inventoryRepository.GetByIdWithTrackingAsync(request.TargetId, cancellationToken)
                ?? throw new KeyNotFoundException($"Inventory item with ID {request.TargetId} not found.");

            itemsToUpdate.Add(inventoryItem); // Add only the specific item
            palletId = inventoryItem.PalletId; // Get pallet ID from the item
            accountId = inventoryItem.AccountId; // Get account from the item

            description = $"Applied {request.LabelType} labeling to {request.QuantityLabeled} units of item LPN {inventoryItem.Barcode}.";
        }

        if (!itemsToUpdate.Any())
        {
            // Should not happen based on checks above, but good safeguard
            throw new InvalidOperationException("No inventory items found to apply labeling to.");
        }


        // Create VAS Transaction for Billing
        var vasTransaction = VASTransaction.Create(
            accountId,
            palletId, // Link VAS to the pallet if available
            ServiceType.Labeling, // Use the specific service type
            request.UserId,
            description);

        // Add lines representing the items processed (input/output quantity are the same)
        // Billing might be per pallet (quantity=1) or per item quantity. We record item details here.
        foreach (var item in itemsToUpdate)
        {
            // Assuming billing 'per each' item labeled, use item quantity.
            // If billing 'per pallet', the quantity in AddLine would be 1.
            // Rate configuration (UoM) determines how this quantity is used.
            vasTransaction.AddInputLine(item.MaterialId, item.Quantity, item.WeightActual.Value);
            vasTransaction.AddOutputLine(item.MaterialId, item.Quantity, item.WeightActual.Value);
        }
        vasTransaction.Complete();
        await vasRepository.AddAsync(vasTransaction, cancellationToken);

        // Update Inventory Status/Flag for each processed item
        foreach (var item in itemsToUpdate)
        {
            item.UpdateComplianceLabelStatus(request.LabelType);
            // Example: If items were waiting for labeling, mark them available now.
            // if(item.Status == InventoryStatus.AwaitingLabeling) item.ReleaseFromQuarantine(); // Using Release as a generic 'make available'
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}