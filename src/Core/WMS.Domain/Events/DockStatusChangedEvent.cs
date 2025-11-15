using MediatR;
using WMS.Domain.Shared;

namespace WMS.Domain.Events;

public record DockStatusChangedEvent(
    Guid DockId,
    bool IsAvailable,
    Guid? AppointmentId) : INotification;