using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Abstractions.Security;
using WMS.Domain.Entities;
using WMS.Domain.Events;
using WMS.Domain.ValueObjects;

public class MaterialReceivedHandler(
    IMaterialInventoryRepository inventoryRepository,
    IReceivingTransactionRepository receivingRepository,
    ILocationRepository locationRepository,
    IUnitOfWork unitOfWork)
    : INotificationHandler<MaterialReceivedEvent>
{
    public async Task Handle(MaterialReceivedEvent notification, CancellationToken cancellationToken)
    {
        var receivingAggregate = await receivingRepository.GetByIdWithDetailsAsync(notification.ReceivingId, cancellationToken)
            ?? throw new InvalidOperationException($"Receiving Transaction record {notification.ReceivingId} not found.");

        var line = receivingAggregate.Pallets
            .SelectMany(p => p.Lines)
            .FirstOrDefault(l => l.Id == notification.PalletLineId)
            ?? throw new InvalidOperationException($"Receiving Line Transaction {notification.PalletLineId} not found.");

        var existingInventory = await inventoryRepository.GetByPalletLineIdAsync(notification.PalletLineId, cancellationToken);

        if (existingInventory != null)
        {
            existingInventory.UpdateDetails(
                line.Quantity,
                Weight.Create(line.Weight, "KG"),
                line.BatchNumber,
                line.ExpiryDate
            );
        }
        else
        {
            var warehouseId = await receivingRepository.GetWarehouseIdForReceivingAsync(notification.ReceivingId, cancellationToken)
                ?? throw new InvalidOperationException("Could not determine warehouse for receiving transaction.");

            var receivingLocation = await locationRepository.GetDefaultReceivingLocationForWarehouseAsync(warehouseId, cancellationToken)
                ?? throw new InvalidOperationException($"No default receiving location found for warehouse {warehouseId}.");

            var newInventory = MaterialInventory.Create(
                line.MaterialId,
                receivingLocation.Id,
                line.PalletId,
                line.Id,
                line.Quantity,
                line.BatchNumber,
                Weight.Create(line.Weight, "KG"),
                line.ExpiryDate,
                line.AccountId,
                line.Barcode     
            );
            await inventoryRepository.AddAsync(newInventory, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}