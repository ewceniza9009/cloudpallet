using MediatR;
using Microsoft.AspNetCore.SignalR;
using WMS.Api.Hubs;
using WMS.Api.Hubs.Interfaces;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Events;

namespace WMS.Api.EventHandlers;

public class DockCheckInNotificationHandler(
    IHubContext<NotificationHub, INotificationHubClient> hubContext,
    IWarehouseRepository warehouseRepository,
    ITruckRepository truckRepository)
    : INotificationHandler<DockCheckInEvent>
{
    public async Task Handle(DockCheckInEvent notification, CancellationToken cancellationToken)
    {
        var truck = await truckRepository.GetByIdAsync(notification.TruckId, cancellationToken);
        var spot = await warehouseRepository.GetYardSpotByIdAsync(notification.AssignedYardSpotId, cancellationToken);

        if (truck is null || spot is null) return;

        var message = $"Truck '{truck.LicensePlate}' has checked in and is assigned to yard spot '{spot.SpotNumber}'.";
        var notificationDto = new NotificationDto("local_shipping", message,DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc));

        await hubContext.Clients.All.ReceiveNotification(notificationDto);
    }
}