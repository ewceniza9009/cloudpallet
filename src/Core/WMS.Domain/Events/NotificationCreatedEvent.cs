using MediatR;
using WMS.Domain.Shared;

namespace WMS.Domain.Events;

public record NotificationCreatedEvent(
    string Icon,
    string Message,
    DateTime Timestamp) : IDomainEvent, INotification;