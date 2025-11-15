using MediatR;
using Microsoft.AspNetCore.SignalR;
using WMS.Api.Hubs;
using WMS.Api.Hubs.Interfaces;
using WMS.Domain.Events;

namespace WMS.Api.EventHandlers;

public class PalletReceivedNotificationHandler(IHubContext<NotificationHub, INotificationHubClient> hubContext)
    : INotificationHandler<PalletReceivedEvent>
{
    public async Task Handle(PalletReceivedEvent notification, CancellationToken cancellationToken)
    {
        var message = $"Pallet '{notification.PalletBarcode}' has been received and is ready for putaway.";
        var notificationDto = new NotificationDto("inventory_2", message, DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc));

        await hubContext.Clients.All.ReceiveNotification(notificationDto);
    }
}