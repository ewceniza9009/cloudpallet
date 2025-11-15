using MediatR;
using WMS.Domain.Shared;

namespace WMS.Domain.Events;

public record WithdrawalCompletedEvent(Guid WithdrawalTransactionId, Guid AccountId) : IDomainEvent, INotification;