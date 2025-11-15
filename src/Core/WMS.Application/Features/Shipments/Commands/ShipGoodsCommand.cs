using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Events;

namespace WMS.Application.Features.Shipments.Commands;

public record ShipGoodsCommand(Guid AppointmentId, string ShipmentNumber, IEnumerable<Guid> PickTransactionIds) : IRequest<Guid>;

public class ShipGoodsCommandHandler(
    IPickTransactionRepository pickRepo,
    IWithdrawalTransactionRepository withdrawalRepo,
    IUnitOfWork unitOfWork,
    IPublisher publisher) : IRequestHandler<ShipGoodsCommand, Guid>
{
    public async Task<Guid> Handle(ShipGoodsCommand request, CancellationToken cancellationToken)
    {
        var picks = await pickRepo.GetByIdsWithDetailsAsync(request.PickTransactionIds, cancellationToken);
        var pickList = picks.ToList();

        if (!pickList.Any())
        {
            throw new InvalidOperationException("Cannot ship with no picked items.");
        }

        var withdrawal = WithdrawalTransaction.Create(request.AppointmentId, request.ShipmentNumber, pickList);

        await withdrawalRepo.AddAsync(withdrawal, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new WithdrawalCompletedEvent(withdrawal.Id, withdrawal.AccountId), cancellationToken);

        return withdrawal.Id;
    }
}