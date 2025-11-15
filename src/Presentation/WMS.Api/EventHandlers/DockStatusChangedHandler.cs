using MediatR;
using Microsoft.AspNetCore.SignalR;
using WMS.Api.Hubs;
using WMS.Api.Hubs.Interfaces;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Events;

namespace WMS.Api.EventHandlers;

public class DockStatusChangedHandler(
    IHubContext<DockHub, IDockHubClient> dockHubContext,
    IHubContext<NotificationHub, INotificationHubClient> notificationHubContext,
    ITruckRepository truckRepository,
    IDockAppointmentRepository appointmentRepository)
    : INotificationHandler<DockStatusChangedEvent>
{
    public async Task Handle(DockStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        var update = new DockStatusUpdate(
            notification.DockId,
            notification.IsAvailable,
            notification.AppointmentId);

        await dockHubContext.Clients.All.ReceiveDockStatusUpdate(update);

        string message;
        if (notification.IsAvailable)
        {
            message = $"Dock is now available.";
        }
        else if (notification.AppointmentId.HasValue)
        {
            var appointment = await appointmentRepository.GetByIdAsync(notification.AppointmentId.Value, cancellationToken);
            if (appointment?.TruckId != null)
            {
                var truck = await truckRepository.GetByIdAsync(appointment.TruckId.Value, cancellationToken);
                message = $"Truck '{truck?.LicensePlate}' has moved to the dock.";
            }
            else
            {
                message = $"A truck has occupied a dock.";
            }
        }
        else
        {
            return;     
        }

        var notificationDto = new NotificationDto("local_shipping", message, DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc));
        await notificationHubContext.Clients.All.ReceiveNotification(notificationDto);
    }
}