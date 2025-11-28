using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.Inventory.Commands;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Inventory.Services;

public class LabelingHandler(
    IReceivingTransactionRepository receivingRepository,
    IMaterialInventoryRepository inventoryRepository,
    IVASTransactionRepository vasRepository,
    IUnitOfWork unitOfWork) : IVasServiceHandler
{
    public ServiceType ServiceType => ServiceType.Labeling;

    public async Task HandleAsync(RecordVasCommand command, CancellationToken cancellationToken)
    {
        if (!command.TargetId.HasValue || !command.TargetType.HasValue || !command.LabelType.HasValue || !command.QuantityLabeled.HasValue)
            throw new InvalidOperationException("TargetId, TargetType, LabelType, and QuantityLabeled are required for Labeling.");

        Guid accountId;
        Guid? palletId = null;
        string description;
        List<MaterialInventory> itemsToUpdate = new();

        if (command.TargetType == TargetType.Pallet)
        {
            var pallet = await receivingRepository.GetPalletWithLinesByIdAsync(command.TargetId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Pallet with ID {command.TargetId.Value} not found.");

            palletId = pallet.Id;
            accountId = pallet.AccountId;
            var inventoryItemsOnPallet = await inventoryRepository.GetByPalletIdAsync(pallet.Id, cancellationToken);
            itemsToUpdate.AddRange(inventoryItemsOnPallet);
            description = $"Applied {command.LabelType} labeling to Pallet {pallet.Barcode} ({itemsToUpdate.Count} items).";
        }
        else // TargetType.InventoryItem
        {
            var inventoryItem = await inventoryRepository.GetByIdWithTrackingAsync(command.TargetId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Inventory item with ID {command.TargetId.Value} not found.");

            itemsToUpdate.Add(inventoryItem);
            palletId = inventoryItem.PalletId;
            accountId = inventoryItem.AccountId;
            description = $"Applied {command.LabelType} labeling to {command.QuantityLabeled} units of item {inventoryItem.Barcode}.";
        }

        if (!itemsToUpdate.Any())
        {
            throw new InvalidOperationException("No inventory items found to apply labeling to.");
        }

        var vasTransaction = VASTransaction.Create(
            accountId,
            palletId,
            ServiceType.Labeling,
            command.UserId,
            description);

        foreach (var item in itemsToUpdate)
        {
            vasTransaction.AddInputLine(item.MaterialId, item.Quantity, item.WeightActual.Value, item.BatchNumber, item.ExpiryDate);
            vasTransaction.AddOutputLine(item.MaterialId, item.Quantity, item.WeightActual.Value, item.BatchNumber, item.ExpiryDate);
        }
        vasTransaction.Complete();
        await vasRepository.AddAsync(vasTransaction, cancellationToken);

        foreach (var item in itemsToUpdate)
        {
            item.UpdateComplianceLabelStatus(command.LabelType.Value);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
