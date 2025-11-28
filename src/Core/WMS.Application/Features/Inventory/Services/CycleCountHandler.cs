using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.Inventory.Commands;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Inventory.Services;

public class CycleCountHandler(
    IMaterialInventoryRepository inventoryRepository,
    IVASTransactionRepository vasRepository,
    IInventoryAdjustmentRepository inventoryAdjustmentRepository,
    IUnitOfWork unitOfWork) : IVasServiceHandler
{
    public ServiceType ServiceType => ServiceType.CycleCount;

    public async Task HandleAsync(RecordVasCommand command, CancellationToken cancellationToken)
    {
        if (command.CountedItems == null || !command.CountedItems.Any() || !command.DurationHours.HasValue)
            throw new InvalidOperationException("CountedItems list and DurationHours are required for Cycle Count.");

        Guid? accountId = null;
        Guid? firstPalletId = null;
        int itemsAdjusted = 0;

        foreach (var countedItem in command.CountedItems)
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

                // 1. Perform the actual inventory adjustment using unified method
                inventory.AdjustInventory(discrepancy, weightAdjustment);

                // 2. Create the formal audit record
                var adjustmentRecord = InventoryAdjustment.Create(
                    inventory.Id,
                    discrepancy,
                    AdjustmentReason.Count,
                    inventory.AccountId,
                    command.UserId
                );
                
                await inventoryAdjustmentRepository.AddAsync(adjustmentRecord, cancellationToken);
            }
        }

        if (accountId.HasValue)
        {
            var description = $"Performed Cycle Count. Duration: {command.DurationHours.Value:F1} hrs. Items checked: {command.CountedItems.Count}. Discrepancies: {itemsAdjusted}.";
            var vasTransaction = VASTransaction.Create(
                accountId.Value,
                firstPalletId,
                ServiceType.CycleCount,
                command.UserId,
                description);

            // Bill based on hours
            vasTransaction.AddInputLine(null, command.DurationHours.Value, 0);
            vasTransaction.Complete();
            await vasRepository.AddAsync(vasTransaction, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
