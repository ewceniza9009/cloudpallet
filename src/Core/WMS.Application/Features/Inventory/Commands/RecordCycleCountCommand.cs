// ---- File: src/Core/WMS.Application/Features/Inventory/Commands/RecordCycleCountCommand.cs ----

using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction; // <-- ADDED
using WMS.Domain.Enums;

namespace WMS.Application.Features.Inventory.Commands;

public record CountItemDto(Guid InventoryId, decimal CountedQuantity);

public record RecordCycleCountCommand(
    List<CountItemDto> CountedItems,
    decimal DurationHours
    ) : IRequest
{
    public Guid UserId { get; init; }
}

public class RecordCycleCountCommandHandler(
    IMaterialInventoryRepository inventoryRepository,
    IVASTransactionRepository vasRepository,
    IInventoryAdjustmentRepository inventoryAdjustmentRepository, // <-- INJECTED
    IUnitOfWork unitOfWork) : IRequestHandler<RecordCycleCountCommand>
{
    public async Task Handle(RecordCycleCountCommand request, CancellationToken cancellationToken)
    {
        if (!request.CountedItems.Any()) return;

        Guid? accountId = null;
        Guid? firstPalletId = null;
        decimal totalWeightAdjusted = 0;
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
                inventory.AdjustForWeighedPick(0, -weightAdjustment); // Adjust weight
                totalWeightAdjusted += weightAdjustment;

                // --- START: IMPLEMENTED AUDIT ---
                // 2. Create the formal audit record
                var adjustmentRecord = InventoryAdjustment.Create(
                    inventory.Id,
                    discrepancy,
                    AdjustmentReason.Count, // The reason is 'Count'
                    inventory.AccountId,
                    request.UserId
                );
                await inventoryAdjustmentRepository.AddAsync(adjustmentRecord, cancellationToken);
                // --- END: IMPLEMENTED AUDIT ---
            }
        }

        if (accountId.HasValue)
        {
            var description = $"Performed Cycle Count. Duration: {request.DurationHours:F1} hrs. Items checked: {request.CountedItems.Count}. Discrepancies found: {itemsAdjusted}.";
            var vasTransaction = VASTransaction.Create(
                accountId.Value,
                firstPalletId,
                ServiceType.CycleCount,
                request.UserId,
                description);

            vasTransaction.AddInputLine(Guid.Empty, request.DurationHours, 0);
            vasTransaction.Complete();
            await vasRepository.AddAsync(vasTransaction, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}