using MediatR;
using Microsoft.AspNetCore.SignalR;
using WMS.Api.Hubs;
using WMS.Api.Hubs.Interfaces;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Events;

namespace WMS.Api.EventHandlers;

public class MaterialReceivedNotificationHandler(
    IHubContext<NotificationHub, INotificationHubClient> hubContext,
    IMaterialRepository materialRepository)
    : INotificationHandler<MaterialReceivedEvent>
{
    public async Task Handle(MaterialReceivedEvent notification, CancellationToken cancellationToken)
    {
        var material = await materialRepository.GetByIdAsync(notification.MaterialId, cancellationToken);
        if (material is null) return;

        var message = $"Material '{material.Name}' has been processed and added to inventory.";
        var notificationDto = new NotificationDto("inventory", message, DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc));

        await hubContext.Clients.All.ReceiveNotification(notificationDto);
    }
}