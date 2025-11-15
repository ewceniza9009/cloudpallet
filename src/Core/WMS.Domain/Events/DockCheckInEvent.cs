using MediatR;
using WMS.Domain.Shared;

namespace WMS.Domain.Events;

public record DockCheckInEvent(Guid AppointmentId, Guid TruckId, Guid AssignedYardSpotId) : IDomainEvent, INotification;