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
                    // NOTE: AdjustForWeighedPick with negative values adds both quantity and weight.
                    // We DO NOT call AdjustQuantity here because it would double-count the quantity addition.
                    
                    // Adjust weight if tracked (or just quantity if weight is 0)
                    if (line.Weight > 0)
                    {
                        existingInventory.AdjustForWeighedPick(-line.Quantity, -line.Weight);
                    }
                    else
                    {
                        // If no weight, we just use AdjustQuantity (which is safe here as we didn't call AdjustForWeighedPick)
                        // But wait, if we have mixed weight/no-weight, we should be consistent.
                        // AdjustForWeighedPick checks weight.
                        // Let's stick to AdjustForWeighedPick with 0 weight if needed, or just AdjustQuantity.
                        // AdjustForWeighedPick: Quantity -= q; Weight -= w;
                        // If w is 0, Weight doesn't change.
                        existingInventory.AdjustForWeighedPick(-line.Quantity, 0);
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
                            line.BatchNumber ?? "RESTORED", // Use stored batch or fallback
                            Weight.Create(line.Weight, "KG"),
                            line.ExpiryDate, // Use stored expiry
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
                    else
                    {
                         // If pallet is empty, we can't determine location. 
                         // STRICT MODE: Throw exception to prevent data loss.
                         throw new InvalidOperationException($"Cannot void transaction: The input pallet for material {line.MaterialId} is empty and its location cannot be determined. Please move at least one item to this pallet to restore the location context.");
                    }
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
