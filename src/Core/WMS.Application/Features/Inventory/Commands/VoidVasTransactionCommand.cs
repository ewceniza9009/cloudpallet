using MediatR;
using System.Text.Json;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Abstractions.Security;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.Inventory.Commands;

public record VoidVasTransactionCommand(
    Guid VasTransactionId,
    string VoidReason
) : IRequest<bool>
{
    public Guid UserId { get; init; }
}

public class VoidVasTransactionCommandHandler(
    IVASTransactionRepository vasRepository,
    IMaterialInventoryRepository inventoryRepository,
    IInventoryAdjustmentRepository adjustmentRepository,
    IVASTransactionAmendmentRepository amendmentRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<VoidVasTransactionCommand, bool>
{
    public async Task<bool> Handle(VoidVasTransactionCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException("User not authenticated.");

        // 1. Get the VAS transaction with lines
        var transaction = await vasRepository.GetByIdWithLinesAsync(request.VasTransactionId, cancellationToken)
            ?? throw new KeyNotFoundException($"VAS Transaction {request.VasTransactionId} not found.");

        // Check if already voided
        if (transaction.IsVoided)
            throw new InvalidOperationException("Transaction is already voided.");

        // 2. Reverse ALL inventory impacts
        var allLines = transaction.GetAllLines();
        var voidDetails = new List<object>();

        // Group lines by material to handle batching if necessary, but processing line-by-line is safer for audit
        foreach (var line in allLines.Where(l => l.MaterialId.HasValue))
        {
            // Record what we're going to reverse
            voidDetails.Add(new
            {
                LineId = line.Id,
                MaterialId = line.MaterialId,
                Quantity = line.Quantity,
                Weight = line.Weight,
                IsInput = line.IsInput
            });

            if (line.IsInput)
            {
                // REVERSE INPUT: This means we CONSUMED items. To void, we must ADD THEM BACK.
                // Strategy: Add back to the transaction's pallet if available.
                
                if (!transaction.PalletId.HasValue)
                {
                    // If no pallet ID on transaction, we can't easily put it back where it came from.
                    // Fallback: This might be a labor-only or special transaction.
                    // For now, we log a warning or skip if no inventory context.
                    // But if it was an INPUT line, it MUST have come from somewhere.
                    // We'll assume it returns to the account's default location or throw if critical.
                    // For this implementation, we require PalletId for inventory reversal.
                    continue; 
                }

                // Find existing inventory of this material on the pallet to merge with
                var existingInventory = (await inventoryRepository.GetByPalletIdAsync(transaction.PalletId.Value, cancellationToken))
                    .FirstOrDefault(i => i.MaterialId == line.MaterialId);

                if (existingInventory != null)
                {
                    // Add back to existing record
                    existingInventory.AdjustQuantity(line.Quantity);
                    // Adjust weight if tracked
                    if (line.Weight > 0)
                    {
                        // We need to add weight back. MaterialInventory.AdjustForWeighedPick subtracts, so we can't use that directly for addition easily without a dedicated method?
                        // Actually MaterialInventory has UpdateDetails but that resets everything.
                        // Let's check if we can manually update weight or if we need a new method.
                        // MaterialInventory.WeightActual is private set.
                        // We might need to use reflection or add a method.
                        // Wait, AdjustForWeighedPick takes positive values to subtract.
                        // We can pass NEGATIVE values to ADD?
                        // Let's check the method: Quantity -= pickedQuantity; Weight -= pickedWeight;
                        // Yes, passing negative values will add.
                        existingInventory.AdjustForWeighedPick(-line.Quantity, -line.Weight);
                    }

                    // Audit
                    await adjustmentRepository.AddAsync(InventoryAdjustment.Create(
                        existingInventory.Id,
                        line.Quantity, // Positive delta = adding back
                        AdjustmentReason.Correction, // Or a specific "Void" reason if available
                        transaction.AccountId,
                        userId
                    ), cancellationToken);
                }
                else
                {
                    // Create new inventory record on the pallet (it was likely fully consumed)
                    // We need a LocationId. We can get it from the Pallet if we had a PalletRepository, 
                    // or assume the transaction stores it? Transaction doesn't.
                    // We can try to find ANY inventory on that pallet to get the LocationId.
                    var anyItemOnPallet = (await inventoryRepository.GetByPalletIdAsync(transaction.PalletId.Value, cancellationToken)).FirstOrDefault();
                    
                    if (anyItemOnPallet != null)
                    {
                        var newInventory = MaterialInventory.Create(
                            line.MaterialId!.Value,
                            anyItemOnPallet.LocationId,
                            transaction.PalletId.Value,
                            Guid.NewGuid(), // New PalletLineId
                            line.Quantity,
                            "RESTORED", // We lost the batch number if not stored in line. TODO: Add Batch to VASTransactionLine
                            Weight.Create(line.Weight, "KG"),
                            null, // Lost expiry
                            transaction.AccountId,
                            $"LPN-VOID-{Guid.NewGuid().ToString().Substring(0, 8)}"
                        );
                        
                        await inventoryRepository.AddAsync(newInventory, cancellationToken);
                        
                        // Audit (for the new record)
                        await adjustmentRepository.AddAsync(InventoryAdjustment.Create(
                            newInventory.Id,
                            line.Quantity,
                            AdjustmentReason.Correction,
                            transaction.AccountId,
                            userId
                        ), cancellationToken);
                    }
                    // If pallet is empty, we can't determine location. This is an edge case.
                }
            }
            else // IsOutput (Produced)
            {
                // REVERSE OUTPUT: This means we CREATED items. To void, we must REMOVE THEM.
                
                if (!transaction.PalletId.HasValue) continue;

                // Find the inventory to remove
                // We look for inventory on the pallet with the same material
                var inventoryCandidates = (await inventoryRepository.GetByPalletIdAsync(transaction.PalletId.Value, cancellationToken))
                    .Where(i => i.MaterialId == line.MaterialId)
                    .OrderByDescending(i => i.Quantity) // Try to find one big enough first
                    .ToList();

                decimal quantityToRemove = line.Quantity;
                decimal weightToRemove = line.Weight;

                foreach (var inv in inventoryCandidates)
                {
                    if (quantityToRemove <= 0) break;

                    decimal qtyToTake = Math.Min(inv.Quantity, quantityToRemove);
                    decimal weightToTake = (qtyToTake / line.Quantity) * line.Weight; // Proportional weight

                    // Check if we are taking everything
                    if (qtyToTake >= inv.Quantity)
                    {
                        // Fully remove this record? 
                        // AdjustForWeighedPick will reduce it to 0.
                        inv.AdjustForWeighedPick(qtyToTake, weightToTake);
                        // Ideally we might delete the record if 0, but keeping it 0 is safer for history.
                    }
                    else
                    {
                        inv.AdjustForWeighedPick(qtyToTake, weightToTake);
                    }

                    // Audit
                    await adjustmentRepository.AddAsync(InventoryAdjustment.Create(
                        inv.Id,
                        -qtyToTake, // Negative delta = removing
                        AdjustmentReason.Correction,
                        transaction.AccountId,
                        userId
                    ), cancellationToken);

                    quantityToRemove -= qtyToTake;
                }

                if (quantityToRemove > 0)
                {
                    // We couldn't find enough inventory to remove!
                    // This implies the produced items were moved or consumed.
                    // STRICT MODE: Throw exception.
                    throw new InvalidOperationException($"Cannot void transaction: Produced inventory for material {line.MaterialId} is missing or insufficient on the pallet. It may have been moved or shipped.");
                }
            }
        }

        // 3. Mark transaction as voided
        transaction.VoidTransaction(userId, request.VoidReason);

        // 4. Create amendment record documenting the void
        var amendmentDetails = JsonSerializer.Serialize(new
        {
            VoidedLines = voidDetails,
            ServiceType = transaction.ServiceType.ToString()
        });

        var amendment = VASTransactionAmendment.CreateVoidAmendment(
            request.VasTransactionId,
            userId,
            request.VoidReason,
            amendmentDetails);

        await amendmentRepository.AddAsync(amendment, cancellationToken);

        // 5. Save changes
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
