using MediatR;
using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Domain.Events;

public record UserRoleChangedEvent(Guid UserId, UserRole NewRole) : IDomainEvent, INotification;