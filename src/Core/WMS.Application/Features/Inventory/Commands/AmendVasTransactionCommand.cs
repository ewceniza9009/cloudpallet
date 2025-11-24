using MediatR;
using System.Text.Json;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Abstractions.Security;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.Inventory.Commands;

public record AmendVasTransactionLineCommand(
    Guid VasTransactionId,
    Guid VasTransactionLineId,
    decimal? NewQuantity,
    decimal? NewWeight,
    string AmendmentReason
) : IRequest<bool>
{
    public Guid UserId { get; init; }
}

public class AmendVasTransactionLineCommandHandler(
    IVASTransactionRepository vasRepository,
    IMaterialInventoryRepository inventoryRepository,
    IInventoryAdjustmentRepository adjustmentRepository,
    IVASTransactionAmendmentRepository amendmentRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<AmendVasTransactionLineCommand, bool>
{
    public async Task<bool> Handle(AmendVasTransactionLineCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException("User not authenticated.");

        // 1. Get the VAS transaction with lines
        var transaction = await vasRepository.GetByIdWithLinesAsync(request.VasTransactionId, cancellationToken)
            ?? throw new KeyNotFoundException($"VAS Transaction {request.VasTransactionId} not found.");

        // Check if transaction is voided
        if (transaction.IsVoided)
            throw new InvalidOperationException("Cannot amend a voided transaction.");

        // 2. Get the specific line to amend
        var line = transaction.GetLineById(request.VasTransactionLineId)
            ?? throw new KeyNotFoundException($"VAS Transaction Line {request.VasTransactionLineId} not found.");

        // 3. Store original values for amendment history
        var originalQuantity = line.Quantity;
        var originalWeight = line.Weight;

        // 4. Determine if line affects inventory
        bool affectsInventory = line.MaterialId.HasValue;

        // 5. If inventory affected, calculate delta and apply changes
        if (affectsInventory)
        {
            await ApplyInventoryDelta(
                transaction,
                line,
                request.NewQuantity ?? line.Quantity,
                request.NewWeight ?? line.Weight,
                userId,
                cancellationToken);
        }

        // 6. Update the line with new values
        if (request.NewQuantity.HasValue && request.NewWeight.HasValue)
        {
            line.AmendQuantityAndWeight(request.NewQuantity.Value, request.NewWeight.Value);
        }
        else if (request.NewQuantity.HasValue)
        {
            line.AmendQuantity(request.NewQuantity.Value);
        }
        else if (request.NewWeight.HasValue)
        {
            line.AmendWeight(request.NewWeight.Value);
        }

        // 7. Create amendment record
        var amendmentDetails = JsonSerializer.Serialize(new
        {
            LineId = line.Id,
            MaterialId = line.MaterialId,
            OriginalQuantity = originalQuantity,
            OriginalWeight = originalWeight,
            NewQuantity = request.NewQuantity ?? originalQuantity,
            NewWeight = request.NewWeight ?? originalWeight
        });

        var amendment = VASTransactionAmendment.CreateLineAmendment(
            request.VasTransactionId,
            userId,
            request.AmendmentReason,
            amendmentDetails);

        await amendmentRepository.AddAsync(amendment, cancellationToken);

        // 8. Save changes
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task ApplyInventoryDelta(
        VASTransaction transaction,
        VASTransactionLine line,
        decimal newQuantity,
        decimal newWeight,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!line.MaterialId.HasValue) return;
        if (!transaction.PalletId.HasValue) return; // Cannot adjust inventory without pallet context

        decimal quantityDelta = newQuantity - line.Quantity;
        decimal weightDelta = newWeight - line.Weight;

        if (quantityDelta == 0) return; // No inventory quantity change (weight change alone usually doesn't trigger stock move, just update)

        if (line.IsInput)
        {
            // INPUT LINE (Consumed Material)
            // Delta > 0: Consumed MORE -> Remove from inventory
            // Delta < 0: Consumed LESS -> Add back to inventory

            if (quantityDelta > 0)
            {
                // Need to consume MORE. Find inventory to take from.
                var inventoryCandidates = (await inventoryRepository.GetByPalletIdAsync(transaction.PalletId.Value, cancellationToken))
                    .Where(i => i.MaterialId == line.MaterialId)
                    .OrderByDescending(i => i.Quantity)
                    .ToList();

                decimal remainingToConsume = quantityDelta;
                decimal weightToConsume = weightDelta; // Simplified: assuming weight delta corresponds to quantity delta

                foreach (var inv in inventoryCandidates)
                {
                    if (remainingToConsume <= 0) break;

                    decimal qtyToTake = Math.Min(inv.Quantity, remainingToConsume);
                    // Proportional weight if not explicitly tracked per unit, or use the delta weight if it's a single item adjustment
                    decimal weightToTake = (qtyToTake / quantityDelta) * weightDelta;

                    inv.AdjustForWeighedPick(qtyToTake, weightToTake);

                    await adjustmentRepository.AddAsync(InventoryAdjustment.Create(
                        inv.Id,
                        -qtyToTake, // Negative = consumed
                        AdjustmentReason.Correction,
                        transaction.AccountId,
                        userId
                    ), cancellationToken);

                    remainingToConsume -= qtyToTake;
                }

                if (remainingToConsume > 0)
                {
                    throw new InvalidOperationException($"Insufficient inventory on pallet to increase consumption by {quantityDelta}.");
                }
            }
            else // quantityDelta < 0
            {
                // Consumed LESS. Add back to inventory.
                // Find existing record to add to
                var existingInventory = (await inventoryRepository.GetByPalletIdAsync(transaction.PalletId.Value, cancellationToken))
                    .FirstOrDefault(i => i.MaterialId == line.MaterialId);

                decimal qtyToReturn = Math.Abs(quantityDelta);
                decimal weightToReturn = Math.Abs(weightDelta);

                if (existingInventory != null)
                {
                    existingInventory.AdjustForWeighedPick(-qtyToReturn, -weightToReturn); // Negative pick = Add
                    
                    await adjustmentRepository.AddAsync(InventoryAdjustment.Create(
                        existingInventory.Id,
                        qtyToReturn, // Positive = returned
                        AdjustmentReason.Correction,
                        transaction.AccountId,
                        userId
                    ), cancellationToken);
                }
                else
                {
                    // Create new record
                     var anyItemOnPallet = (await inventoryRepository.GetByPalletIdAsync(transaction.PalletId.Value, cancellationToken)).FirstOrDefault();
                     if (anyItemOnPallet != null)
                     {
                         var newInventory = MaterialInventory.Create(
                             line.MaterialId.Value,
                             anyItemOnPallet.LocationId,
                             transaction.PalletId.Value,
                             Guid.NewGuid(),
                             qtyToReturn,
                             "RESTORED",
                             Weight.Create(weightToReturn, "KG"),
                             null,
                             transaction.AccountId,
                             $"LPN-AMEND-{Guid.NewGuid().ToString().Substring(0, 8)}"
                         );
                         await inventoryRepository.AddAsync(newInventory, cancellationToken);

                         await adjustmentRepository.AddAsync(InventoryAdjustment.Create(
                             newInventory.Id,
                             qtyToReturn,
                             AdjustmentReason.Correction,
                             transaction.AccountId,
                             userId
                         ), cancellationToken);
                     }
                }
            }
        }
        else // OUTPUT LINE (Produced Material)
        {
            // OUTPUT LINE
            // Delta > 0: Produced MORE -> Add to inventory
            // Delta < 0: Produced LESS -> Remove from inventory

            if (quantityDelta > 0)
            {
                // Produced MORE. Add to inventory.
                var existingInventory = (await inventoryRepository.GetByPalletIdAsync(transaction.PalletId.Value, cancellationToken))
                    .FirstOrDefault(i => i.MaterialId == line.MaterialId);
                
                if (existingInventory != null)
                {
                    existingInventory.AdjustForWeighedPick(-quantityDelta, -weightDelta); // Negative pick = Add
                    
                    await adjustmentRepository.AddAsync(InventoryAdjustment.Create(
                        existingInventory.Id,
                        quantityDelta,
                        AdjustmentReason.Correction,
                        transaction.AccountId,
                        userId
                    ), cancellationToken);
                }
                else
                {
                    // Create new record
                    var anyItemOnPallet = (await inventoryRepository.GetByPalletIdAsync(transaction.PalletId.Value, cancellationToken)).FirstOrDefault();
                    if (anyItemOnPallet != null)
                    {
                        var newInventory = MaterialInventory.Create(
                            line.MaterialId.Value,
                            anyItemOnPallet.LocationId,
                            transaction.PalletId.Value,
                            Guid.NewGuid(),
                            quantityDelta,
                            "CORRECTION",
                            Weight.Create(weightDelta, "KG"),
                            null,
                            transaction.AccountId,
                            $"LPN-AMEND-{Guid.NewGuid().ToString().Substring(0, 8)}"
                        );
                        await inventoryRepository.AddAsync(newInventory, cancellationToken);
                        
                        await adjustmentRepository.AddAsync(InventoryAdjustment.Create(
                            newInventory.Id,
                            quantityDelta,
                            AdjustmentReason.Correction,
                            transaction.AccountId,
                            userId
                        ), cancellationToken);
                    }
                }
            }
            else // quantityDelta < 0
            {
                // Produced LESS. Remove from inventory.
                decimal qtyToRemove = Math.Abs(quantityDelta);
                decimal weightToRemove = Math.Abs(weightDelta);

                var inventoryCandidates = (await inventoryRepository.GetByPalletIdAsync(transaction.PalletId.Value, cancellationToken))
                    .Where(i => i.MaterialId == line.MaterialId)
                    .OrderByDescending(i => i.Quantity)
                    .ToList();

                foreach (var inv in inventoryCandidates)
                {
                    if (qtyToRemove <= 0) break;

                    decimal qtyToTake = Math.Min(inv.Quantity, qtyToRemove);
                    decimal weightToTake = (qtyToTake / qtyToRemove) * weightToRemove;

                    inv.AdjustForWeighedPick(qtyToTake, weightToTake);

                    await adjustmentRepository.AddAsync(InventoryAdjustment.Create(
                        inv.Id,
                        -qtyToTake,
                        AdjustmentReason.Correction,
                        transaction.AccountId,
                        userId
                    ), cancellationToken);

                    qtyToRemove -= qtyToTake;
                }

                if (qtyToRemove > 0)
                {
                    throw new InvalidOperationException($"Cannot reduce production quantity: Inventory for material {line.MaterialId} is missing or insufficient.");
                }
            }
        }
    }
}
