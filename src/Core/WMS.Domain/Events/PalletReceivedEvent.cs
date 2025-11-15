using MediatR;
using WMS.Domain.Shared;

namespace WMS.Domain.Events;

public record PalletReceivedEvent(
    Guid PalletId,
    Guid AccountId,
    Guid ReceivingLocationId,
    string PalletBarcode) : IDomainEvent, INotification;