using MediatR;   
using WMS.Domain.Shared;

namespace WMS.Domain.Events;

public record DockAppointmentScheduledEvent(Guid AppointmentId, Guid DockId, DateTime StartDateTime, DateTime EndDateTime) : IDomainEvent, INotification;