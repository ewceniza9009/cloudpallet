using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities.Transaction;

namespace WMS.Application.Features.Inventory.Commands;

public record CreateReceivingSessionCommand(Guid SupplierId, Guid DockAppointmentId, Guid AccountId, string? Remarks) : IRequest<Guid>;

public class CreateReceivingSessionCommandHandler(
    IReceivingTransactionRepository receivingRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateReceivingSessionCommand, Guid>
{
    public async Task<Guid> Handle(CreateReceivingSessionCommand request, CancellationToken cancellationToken)
    {
        var receiving = Receiving.Create(request.SupplierId, request.DockAppointmentId, request.AccountId, request.Remarks);

        await receivingRepository.AddAsync(receiving, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return receiving.Id;
    }
}
