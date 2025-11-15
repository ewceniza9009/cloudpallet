// ---- File: src/Presentation/WMS.Api/EventHandlers/UserRoleChangedNotificationHandler.cs ----
using MediatR;
using Microsoft.AspNetCore.SignalR;
using WMS.Api.Hubs;
using WMS.Api.Hubs.Interfaces;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Events;

namespace WMS.Api.EventHandlers;

public class UserRoleChangedNotificationHandler(
    IHubContext<NotificationHub, INotificationHubClient> hubContext,
    IUserRepository userRepository)
    : INotificationHandler<UserRoleChangedEvent>
{
    public async Task Handle(UserRoleChangedEvent notification, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(notification.UserId, cancellationToken);
        if (user is null) return;

        var message = $"The role for user '{user.UserName}' has been changed to '{notification.NewRole}'.";
        var notificationDto = new NotificationDto("manage_accounts", message, DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc));

        await hubContext.Clients.All.ReceiveNotification(notificationDto);
    }
}