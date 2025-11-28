// ---- File: src/Core/WMS.Application/Features/Inventory/Commands/CreateKitCommand.cs ----

using MediatR;
using WMS.Application.Abstractions.Integrations;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Abstractions.Security;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.Inventory.Commands;

/// <summary>
/// DTO representing a single component to be consumed.
/// </summary>
public record KitComponentDto(
    Guid ComponentMaterialId,
    Guid SourceInventoryId,
    decimal QuantityToConsume); // The total quantity of this component to consume

/// <summary>
/// Command to create a new Kit from multiple component inventories.
/// </summary>
public record CreateKitCommand(
    Guid TargetKitMaterialId, // The Kit SKU to create
    decimal QuantityToBuild,  // How many Kits to create
    decimal DurationHours,    // For billing labor
    List<KitComponentDto> Components // The list of source items to consume
    ) : IRequest<Guid> // Returns the new MaterialInventory ID of the Kit
{
    public Guid UserId { get; init; }
}

public class CreateKitCommandHandler(
    IMaterialRepository materialRepository,
    IBillOfMaterialRepository bomRepository,
    IMaterialInventoryRepository inventoryRepository,
    IVASTransactionRepository vasRepository,
    IBarcodeGenerationService barcodeService,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateKitCommand, Guid>
{
    public async Task<Guid> Handle(CreateKitCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? Guid.Empty;

        // 1. Get the Kit's Recipe (BOM)
        var bom = await bomRepository.GetByOutputMaterialIdAsync(request.TargetKitMaterialId, cancellationToken)
            ?? throw new InvalidOperationException($"No Bill of Materials (Kit Recipe) found for target material {request.TargetKitMaterialId}.");

        var targetMaterial = await materialRepository.GetByIdAsync(request.TargetKitMaterialId, cancellationToken)
            ?? throw new KeyNotFoundException("Target Kit material not found.");

        Guid accountId = Guid.Empty;
        Guid locationId = Guid.Empty;
        Guid palletId = Guid.Empty;
        string batchNumber = $"KIT-{DateTime.UtcNow.Ticks}";
        DateTime? expiryDate = null;

        // --- START: MODIFIED LOGIC ---
        // Create the VAS transaction with empty IDs first
        var vasTransaction = VASTransaction.Create(
            Guid.Empty, // Placeholder
            null,       // Placeholder
            ServiceType.Kitting,
            userId,
            $"Assembled {request.QuantityToBuild} units of Kit '{targetMaterial.Name}'.");
        // --- END: MODIFIED LOGIC ---

        // 2. Debit (Consume) all source components
        foreach (var component in request.Components)
        {
            var inventorySource = await inventoryRepository.GetByIdWithTrackingAsync(component.SourceInventoryId, cancellationToken)
                ?? throw new KeyNotFoundException($"Source inventory {component.SourceInventoryId} for component {component.ComponentMaterialId} not found.");

            if (inventorySource.Quantity < component.QuantityToConsume)
                throw new InvalidOperationException($"Insufficient quantity for component {component.ComponentMaterialId}. Required: {component.QuantityToConsume}, Available: {inventorySource.Quantity}.");

            // Set Account, Location, and Pallet from the *first* component
            if (accountId == Guid.Empty)
            {
                accountId = inventorySource.AccountId;
                locationId = inventorySource.LocationId;
                palletId = inventorySource.PalletId;
                expiryDate = inventorySource.ExpiryDate;

                // --- START: USE SETTER METHODS ---
                vasTransaction.SetAccount(accountId);
                vasTransaction.SetPallet(palletId);
                // --- END: USE SETTER METHODS ---
            }

            // Calculate proportional weight
            var weightToDebit = (inventorySource.WeightActual.Value / inventorySource.Quantity) * component.QuantityToConsume;

            // Adjust inventory
            inventorySource.AdjustForWeighedPick(component.QuantityToConsume, weightToDebit);

            // Log to VAS
            vasTransaction.AddInputLine(component.ComponentMaterialId, component.QuantityToConsume, weightToDebit, inventorySource.BatchNumber, inventorySource.ExpiryDate);
        }

        // 3. Credit (Create) the new Kit Inventory
        decimal outputWeight = targetMaterial.BaseWeight * request.QuantityToBuild;

        var newKitInventory = MaterialInventory.Create(
            targetMaterial.Id,
            locationId,  // Create kit in the same location as components
            palletId,    // Add to the same pallet
            Guid.NewGuid(), // Not from a pallet line
            request.QuantityToBuild,
            batchNumber,
            Weight.Create(outputWeight, "KG"),
            expiryDate, // Inherits expiry from first component (can be changed)
            accountId,
            barcodeService.GenerateItemBarcode(targetMaterial.Id) // Generate a new LPN
        );

        await inventoryRepository.AddAsync(newKitInventory, cancellationToken);

        // Log output to VAS
        vasTransaction.AddOutputLine(targetMaterial.Id, request.QuantityToBuild, outputWeight, batchNumber, expiryDate);

        // 4. Add Labor Line for Billing
        if (request.DurationHours > 0)
        {
            // Use Guid.Empty for MaterialId to signify a non-material (labor) line
            // Store the hours in the 'Quantity' field.
            vasTransaction.AddInputLine(null, request.DurationHours, 0);
        }

        // 5. Save all changes
        vasTransaction.Complete();
        await vasRepository.AddAsync(vasTransaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return newKitInventory.Id; // Return the ID of the new kit inventory
    }
}