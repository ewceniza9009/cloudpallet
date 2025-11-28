// ---- File: src/Core/WMS.Application/Features/Shipments/Commands/CreatePickListCommand.cs ----

using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Entities; // <-- ADDED
using WMS.Domain.Enums; // <-- ADDED

namespace WMS.Application.Features.Shipments.Commands;

public record CreatePickListCommand(
    Dictionary<Guid, decimal> OrderItems,
    Guid UserId,
    bool IsExpedited
    ) : IRequest<IEnumerable<Guid>>;

public class CreatePickListCommandHandler(
    IMaterialInventoryRepository inventoryRepository,
    IPickTransactionRepository pickTransactionRepository,
    IVASTransactionRepository vasRepository, // <-- ADDED
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePickListCommand, IEnumerable<Guid>>
{
    public async Task<IEnumerable<Guid>> Handle(CreatePickListCommand request, CancellationToken cancellationToken)
    {
        var createdPicks = new List<PickTransaction>();

        if (request.OrderItems == null || !request.OrderItems.Any())
        {
            throw new ArgumentException("Order items cannot be empty.");
        }

        Guid? firstAccountId = null;

        // --- START: ADDED VAS TRANSACTION FOR EXPEDITED ---
        // We must determine the AccountId *before* creating picks
        // We'll peek at the first item's inventory to get the account
        var firstItem = request.OrderItems.First();
        var firstInventorySources = (await inventoryRepository.FindFifoInventoryForMaterialAsync(firstItem.Key, cancellationToken)).ToList();
        if (!firstInventorySources.Any())
        {
            throw new InvalidOperationException($"Insufficient total inventory available for material ID {firstItem.Key}.");
        }
        firstAccountId = firstInventorySources.First().AccountId;

        if (request.IsExpedited)
        {
            var description = $"Expedited Handling Surcharge for Pick List created at {DateTime.UtcNow:g}";
            var vasTransaction = VASTransaction.Create(
                firstAccountId.Value,
                null, // Not tied to a specific pallet
                ServiceType.Surcharge,
                request.UserId,
                description);

            // Add a single line item representing 1 shipment/order
            vasTransaction.AddInputLine(Guid.Empty, 1, 0); // 1 Shipment
            vasTransaction.Complete();

            await vasRepository.AddAsync(vasTransaction, cancellationToken);
        }
        // --- END: ADDED VAS TRANSACTION ---


        foreach (var orderItem in request.OrderItems)
        {
            var materialId = orderItem.Key;
            var quantityToFulfill = orderItem.Value;

            if (quantityToFulfill <= 0)
            {
                throw new ArgumentException($"Quantity for material {materialId} must be positive.");
            }

            // Re-fetch or use previously fetched inventory
            var availableInventory = materialId == firstItem.Key
                ? firstInventorySources
                : (await inventoryRepository.FindFifoInventoryForMaterialAsync(materialId, cancellationToken)).ToList();

            if (availableInventory.Sum(inv => inv.Quantity) < quantityToFulfill)
            {
                throw new InvalidOperationException($"Insufficient total inventory available for material ID {materialId}. Required: {quantityToFulfill}, Available: {availableInventory.Sum(inv => inv.Quantity)}.");
            }

            foreach (var inventorySource in availableInventory)
            {
                if (inventorySource.AccountId != firstAccountId)
                {
                    throw new InvalidOperationException("All items in a pick list must belong to the same account.");
                }

                if (quantityToFulfill <= 0) break;

                var quantityToPickFromThisSource = Math.Min(inventorySource.Quantity, quantityToFulfill);

                // The IsExpedited flag is still set on the PickTransaction for operational logic
                var pick = PickTransaction.Create(
                    inventorySource.Id,
                    quantityToPickFromThisSource,
                    request.UserId,
                    inventorySource.AccountId,
                    inventorySource.BatchNumber,
                    inventorySource.ExpiryDate,
                    request.IsExpedited); // <-- PASS FLAG HERE

                createdPicks.Add(pick);
                await pickTransactionRepository.AddAsync(pick, cancellationToken);

                quantityToFulfill -= quantityToPickFromThisSource;
            }

            if (quantityToFulfill > 0)
            {
                throw new InvalidOperationException($"Logic error: Could not fulfill quantity for material ID {materialId} despite initial availability check.");
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return createdPicks.Select(p => p.Id);
    }
}