namespace WMS.Api.Hubs.Interfaces;

public record NotificationDto(
    string Icon,
    string Text,
    DateTime Time);

public interface INotificationHubClient
{
    Task ReceiveNotification(NotificationDto notification);
}