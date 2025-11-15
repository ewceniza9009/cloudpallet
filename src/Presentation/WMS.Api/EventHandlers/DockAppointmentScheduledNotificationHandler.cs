using MediatR;
using Microsoft.AspNetCore.SignalR;
using WMS.Api.Hubs;
using WMS.Api.Hubs.Interfaces;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Events;

namespace WMS.Api.EventHandlers;

public class DockAppointmentScheduledNotificationHandler(
    IHubContext<NotificationHub, INotificationHubClient> hubContext,
    IDockAppointmentRepository appointmentRepository)
    : INotificationHandler<DockAppointmentScheduledEvent>
{
    public async Task Handle(DockAppointmentScheduledEvent notification, CancellationToken cancellationToken)
    {
        var appointment = await appointmentRepository.GetByIdAsync(notification.AppointmentId, cancellationToken);
        if (appointment?.Truck is null) return;

        var message = $"New appointment scheduled for truck '{appointment.Truck.LicensePlate}' at {DateTime.SpecifyKind(notification.StartDateTime, DateTimeKind.Utc)}.";
        var notificationDto = new NotificationDto("event_available", message, DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc));

        await hubContext.Clients.All.ReceiveNotification(notificationDto);
    }
}