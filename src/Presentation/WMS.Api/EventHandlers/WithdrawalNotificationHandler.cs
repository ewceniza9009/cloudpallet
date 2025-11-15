using MediatR;
using Microsoft.AspNetCore.SignalR;
using WMS.Api.Hubs;
using WMS.Api.Hubs.Interfaces;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Events;

namespace WMS.Api.EventHandlers;

public class WithdrawalCompletedNotificationHandler(
    IHubContext<NotificationHub, INotificationHubClient> hubContext,
    IWithdrawalTransactionRepository withdrawalRepository)
    : INotificationHandler<WithdrawalCompletedEvent>
{
    public async Task Handle(WithdrawalCompletedEvent notification, CancellationToken cancellationToken)
    {
        var withdrawal = await withdrawalRepository.GetByIdWithDetailsAsync(notification.WithdrawalTransactionId, cancellationToken);
        if (withdrawal is null) return;

        var message = $"Shipment '{withdrawal.ShipmentNumber}' for account '{withdrawal.Account.Name}' has been completed.";
        var notificationDto = new NotificationDto("local_shipping", message, DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc));

        await hubContext.Clients.All.ReceiveNotification(notificationDto);
    }
}