using MediatR;
using WMS.Domain.Shared;

namespace WMS.Domain.Events;

public record MaterialReceivedEvent(Guid ReceivingId, Guid PalletId, Guid PalletLineId, Guid MaterialId, Guid AccountId) : IDomainEvent, INotification;