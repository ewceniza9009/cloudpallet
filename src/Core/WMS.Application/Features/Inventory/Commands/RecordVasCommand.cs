// ---- File: src/Core/WMS.Application/Features/Inventory/Commands/RecordVasCommand.cs ----

using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.Inventory.Commands;

// Enum to specify whether the labeling applies to a whole pallet or a specific item
public enum TargetType { Pallet, InventoryItem }

public record RecordVasCommand(
    Guid? PalletId,
    ServiceType ServiceType,
    Guid? SourceInventoryId,
    Guid? TargetMaterialId,
    decimal? QuantityToProcess,
    // --- Fields specifically for Labeling ---
    Guid? TargetId,
    TargetType? TargetType,
    ComplianceLabelType? LabelType,
    decimal? QuantityLabeled,
    // --- Fields specifically for Fumigation ---
    Guid? InventoryId, // Target for Fumigation
    decimal? DurationHours,
    // --- Fields specifically for Cycle Count ---
    List<CountItemDto>? CountedItems
    ) : IRequest
{
    public Guid UserId { get; init; }
}

public class RecordVasCommandHandler(
    // --- CONSTRUCTOR MODIFIED ---
    IReceivingTransactionRepository receivingRepository,
    IMaterialInventoryRepository inventoryRepository,
    IVASTransactionRepository vasRepository,
    IBillOfMaterialRepository bomRepository,
    IMaterialRepository materialRepository,
    IInventoryAdjustmentRepository inventoryAdjustmentRepository, // <-- ADDED
    IUnitOfWork unitOfWork)
    // --- END MODIFICATION ---
    : IRequestHandler<RecordVasCommand>
{
    public async Task Handle(RecordVasCommand request, CancellationToken cancellationToken)
    {
        switch (request.ServiceType)
        {
            case ServiceType.Blasting:
                await HandleBlastingAsync(request, cancellationToken);
                break;
            case ServiceType.Repack:
                await HandleRepackingAsync(request, cancellationToken);
                break;
            case ServiceType.Split:
                // Note: 'Split' is currently handled by its own command (TransferItemsToNewPalletCommandHandler)
                // If it were to be handled here, its logic would be added.
                throw new NotSupportedException("ServiceType.Split is handled by the TransferItems command, not RecordVas.");

            case ServiceType.Labeling:
                await HandleLabelingAsync(request, cancellationToken);
                break;

            case ServiceType.Fumigation:
                await HandleFumigationAsync(request, cancellationToken);
                break;

            case ServiceType.CycleCount:
                await HandleCycleCountAsync(request, cancellationToken);
                break;

            default:
                // This is the line throwing your error
                throw new NotSupportedException("This VAS type is not supported for direct recording.");
        }
    }

    private async Task HandleBlastingAsync(RecordVasCommand request, CancellationToken cancellationToken)
    {
        if (!request.PalletId.HasValue) throw new InvalidOperationException("PalletId is required for Blasting.");

        var pallet = await receivingRepository.GetPalletWithLinesByIdAsync(request.PalletId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("Pallet not found.");

        var description = $"Performed Blast Freezing on pallet {pallet.Barcode}.";

        var vasTransaction = VASTransaction.Create(
            pallet.AccountId,
            pallet.Id,
            ServiceType.Blasting,
            request.UserId,
            description);

        foreach (var line in pallet.Lines)
        {
            vasTransaction.AddInputLine(line.MaterialId, line.Quantity, line.Weight);
        }

        vasTransaction.Complete();
        await vasRepository.AddAsync(vasTransaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleRepackingAsync(RecordVasCommand request, CancellationToken cancellationToken)
    {
        if (!request.SourceInventoryId.HasValue || !request.TargetMaterialId.HasValue || !request.QuantityToProcess.HasValue)
            throw new InvalidOperationException("Source Inventory, Target Material, and Quantity are required for Repacking.");

        var bom = await bomRepository.GetByOutputMaterialIdAsync(request.TargetMaterialId.Value, cancellationToken)
            ?? throw new InvalidOperationException($"No repackaging recipe (Bill of Materials) found for the target material ID {request.TargetMaterialId.Value}.");

        if (bom.Lines.Count != 1)
        {
            throw new NotSupportedException("The system currently only supports single-input repackaging.");
        }
        var bomLine = bom.Lines.Single();

        var sourceInventory = await inventoryRepository.GetByIdWithTrackingAsync(request.SourceInventoryId.Value, cancellationToken) // Use tracking
            ?? throw new KeyNotFoundException("Source inventory not found.");

        var sourceMaterial = await materialRepository.GetByIdAsync(sourceInventory.MaterialId, cancellationToken)
            ?? throw new KeyNotFoundException("Source material not found.");

        if (sourceInventory.MaterialId != bomLine.InputMaterialId)
        {
            throw new InvalidOperationException("The selected source inventory material does not match the required input material in the repackaging recipe.");
        }

        if (sourceInventory.Quantity < request.QuantityToProcess.Value)
            throw new InvalidOperationException("Insufficient quantity in source inventory to perform repackaging.");

        decimal outputQuantity = (request.QuantityToProcess.Value / bomLine.InputQuantity) * bom.OutputQuantity;
        decimal inputWeight = request.QuantityToProcess.Value * sourceMaterial.BaseWeight;

        var targetMaterial = await materialRepository.GetByIdAsync(request.TargetMaterialId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("Target material not found.");
        decimal outputWeight = outputQuantity * targetMaterial.BaseWeight;

        var description = $"Repacked {request.QuantityToProcess.Value} of '{sourceMaterial.Name}' into {outputQuantity} of '{targetMaterial.Name}'";

        var vasTransaction = VASTransaction.Create(
            sourceInventory.AccountId,
            sourceInventory.PalletId,
            ServiceType.Repack,
            request.UserId,
            description);

        vasTransaction.AddInputLine(sourceInventory.MaterialId, request.QuantityToProcess.Value, inputWeight);
        vasTransaction.AddOutputLine(request.TargetMaterialId.Value, outputQuantity, outputWeight);

        // Adjust source inventory
        sourceInventory.AdjustForWeighedPick(request.QuantityToProcess.Value, inputWeight);

        // Create new inventory for the repacked item
        var newInventory = MaterialInventory.Create(
            request.TargetMaterialId.Value,
            sourceInventory.LocationId, // Stays in the same location for now
            sourceInventory.PalletId,   // Added to the same pallet
            Guid.NewGuid(), // This is not tied to a pallet line, so we generate a new ID
            outputQuantity,
            sourceInventory.BatchNumber, // Inherits batch
            Weight.Create(outputWeight, "KG"),
            sourceInventory.ExpiryDate, // Inherits expiry
            sourceInventory.AccountId,
            $"LPN-REPACK-{Guid.NewGuid().ToString().Substring(0, 8)}"); // New LPN

        await inventoryRepository.AddAsync(newInventory, cancellationToken);
        await vasRepository.AddAsync(vasTransaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleLabelingAsync(RecordVasCommand request, CancellationToken cancellationToken)
    {
        if (!request.TargetId.HasValue || !request.TargetType.HasValue || !request.LabelType.HasValue || !request.QuantityLabeled.HasValue)
            throw new InvalidOperationException("TargetId, TargetType, LabelType, and QuantityLabeled are required for Labeling.");

        Guid accountId;
        Guid? palletId = null;
        string description;
        List<MaterialInventory> itemsToUpdate = new();

        if (request.TargetType == TargetType.Pallet)
        {
            var pallet = await receivingRepository.GetPalletWithLinesByIdAsync(request.TargetId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Pallet with ID {request.TargetId.Value} not found.");

            palletId = pallet.Id;
            accountId = pallet.AccountId;
            var inventoryItemsOnPallet = await inventoryRepository.GetByPalletIdAsync(pallet.Id, cancellationToken);
            itemsToUpdate.AddRange(inventoryItemsOnPallet);
            description = $"Applied {request.LabelType} labeling to Pallet {pallet.Barcode} ({itemsToUpdate.Count} items).";
        }
        else // TargetType.InventoryItem
        {
            var inventoryItem = await inventoryRepository.GetByIdWithTrackingAsync(request.TargetId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Inventory item with ID {request.TargetId.Value} not found.");

            itemsToUpdate.Add(inventoryItem);
            palletId = inventoryItem.PalletId;
            accountId = inventoryItem.AccountId;
            description = $"Applied {request.LabelType} labeling to {request.QuantityLabeled} units of item {inventoryItem.Barcode}.";
        }

        if (!itemsToUpdate.Any())
        {
            throw new InvalidOperationException("No inventory items found to apply labeling to.");
        }

        var vasTransaction = VASTransaction.Create(
            accountId,
            palletId,
            ServiceType.Labeling,
            request.UserId,
            description);

        foreach (var item in itemsToUpdate)
        {
            vasTransaction.AddInputLine(item.MaterialId, item.Quantity, item.WeightActual.Value);
            vasTransaction.AddOutputLine(item.MaterialId, item.Quantity, item.WeightActual.Value);
        }
        vasTransaction.Complete();
        await vasRepository.AddAsync(vasTransaction, cancellationToken);

        foreach (var item in itemsToUpdate)
        {
            item.UpdateComplianceLabelStatus(request.LabelType.Value);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleFumigationAsync(RecordVasCommand request, CancellationToken cancellationToken)
    {
        if (!request.InventoryId.HasValue || !request.DurationHours.HasValue)
            throw new InvalidOperationException("InventoryId and DurationHours are required for Fumigation.");

        var inventory = await inventoryRepository.GetByIdWithTrackingAsync(request.InventoryId.Value, cancellationToken)
            ?? throw new KeyNotFoundException($"Inventory item with ID {request.InventoryId.Value} not found.");

        if (inventory.Status != InventoryStatus.Quarantined)
        {
            throw new InvalidOperationException("Inventory item is not currently quarantined.");
        }

        inventory.ReleaseFromQuarantine();

        var description = $"Completed Fumigation for item {inventory.Barcode}. Duration: {request.DurationHours.Value:F1} hrs.";
        var vasTransaction = VASTransaction.Create(
            inventory.AccountId,
            inventory.PalletId,
            ServiceType.Fumigation,
            request.UserId,
            description);

        // Bill per cycle
        vasTransaction.AddInputLine(inventory.MaterialId, 1, 0); // Quantity = 1 cycle

        vasTransaction.Complete();

        await vasRepository.AddAsync(vasTransaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleCycleCountAsync(RecordVasCommand request, CancellationToken cancellationToken)
    {
        if (request.CountedItems == null || !request.CountedItems.Any() || !request.DurationHours.HasValue)
            throw new InvalidOperationException("CountedItems list and DurationHours are required for Cycle Count.");

        Guid? accountId = null;
        Guid? firstPalletId = null;
        int itemsAdjusted = 0;

        foreach (var countedItem in request.CountedItems)
        {
            var inventory = await inventoryRepository.GetByIdWithTrackingAsync(countedItem.InventoryId, cancellationToken)
                ?? throw new KeyNotFoundException($"Inventory item {countedItem.InventoryId} not found during cycle count.");

            accountId ??= inventory.AccountId;
            firstPalletId ??= inventory.PalletId;
            var discrepancy = countedItem.CountedQuantity - inventory.Quantity;

            if (discrepancy != 0)
            {
                itemsAdjusted++;
                var weightPerUnit = inventory.Quantity == 0 ? 0 : inventory.WeightActual.Value / inventory.Quantity;
                var weightAdjustment = discrepancy * weightPerUnit;

                // 1. Perform the actual inventory adjustment
                inventory.AdjustQuantity(discrepancy);
                inventory.AdjustForWeighedPick(0, -weightAdjustment);

                // --- IMPLEMENTED AUDIT ---
                // 2. Create the formal audit record
                var adjustmentRecord = InventoryAdjustment.Create(
                    inventory.Id,
                    discrepancy,
                    AdjustmentReason.Count,
                    inventory.AccountId,
                    request.UserId
                );
                // The repository is now injected and available
                await inventoryAdjustmentRepository.AddAsync(adjustmentRecord, cancellationToken);
                // --- END AUDIT ---
            }
        }

        if (accountId.HasValue)
        {
            var description = $"Performed Cycle Count. Duration: {request.DurationHours.Value:F1} hrs. Items checked: {request.CountedItems.Count}. Discrepancies: {itemsAdjusted}.";
            var vasTransaction = VASTransaction.Create(
                accountId.Value,
                firstPalletId,
                ServiceType.CycleCount,
                request.UserId,
                description);

            // Bill based on hours
            vasTransaction.AddInputLine(null, request.DurationHours.Value, 0);
            vasTransaction.Complete();
            await vasRepository.AddAsync(vasTransaction, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}