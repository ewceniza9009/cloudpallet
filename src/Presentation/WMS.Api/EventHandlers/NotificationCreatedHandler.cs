using MediatR;
using Microsoft.AspNetCore.SignalR;
using WMS.Api.Hubs;
using WMS.Api.Hubs.Interfaces;
using WMS.Domain.Events;

namespace WMS.Api.EventHandlers;

public class NotificationCreatedHandler(IHubContext<NotificationHub, INotificationHubClient> hubContext)
    : INotificationHandler<NotificationCreatedEvent>
{
    public async Task Handle(NotificationCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        var notificationDto = new NotificationDto(
            domainEvent.Icon,
            domainEvent.Message,
            domainEvent.Timestamp);

        await hubContext.Clients.All.ReceiveNotification(notificationDto);
    }
}