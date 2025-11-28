using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.Inventory.Commands;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Inventory.Services;

public class FumigationHandler(
    IMaterialInventoryRepository inventoryRepository,
    IVASTransactionRepository vasRepository,
    IUnitOfWork unitOfWork) : IVasServiceHandler
{
    public ServiceType ServiceType => ServiceType.Fumigation;

    public async Task HandleAsync(RecordVasCommand command, CancellationToken cancellationToken)
    {
        if (!command.InventoryId.HasValue || !command.DurationHours.HasValue)
            throw new InvalidOperationException("InventoryId and DurationHours are required for Fumigation.");

        var inventory = await inventoryRepository.GetByIdWithTrackingAsync(command.InventoryId.Value, cancellationToken)
            ?? throw new KeyNotFoundException($"Inventory item with ID {command.InventoryId.Value} not found.");

        if (inventory.Status != InventoryStatus.Quarantined)
        {
            throw new InvalidOperationException("Inventory item is not currently quarantined.");
        }

        inventory.ReleaseFromQuarantine();

        var description = $"Completed Fumigation for item {inventory.Barcode}. Duration: {command.DurationHours.Value:F1} hrs.";
        var vasTransaction = VASTransaction.Create(
            inventory.AccountId,
            inventory.PalletId,
            ServiceType.Fumigation,
            command.UserId,
            description);

        // Bill per cycle
        vasTransaction.AddInputLine(inventory.MaterialId, 1, 0, inventory.BatchNumber, inventory.ExpiryDate); // Quantity = 1 cycle

        vasTransaction.Complete();

        await vasRepository.AddAsync(vasTransaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
