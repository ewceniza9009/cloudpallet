using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Shipments.Commands;

public record DeletePickItemCommand(Guid PickTransactionId) : IRequest;

public class DeletePickItemCommandHandler(
    IPickTransactionRepository pickRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePickItemCommand>
{
    public async Task Handle(DeletePickItemCommand request, CancellationToken cancellationToken)
    {
        var pickTransaction = await pickRepository.GetByIdAsync(request.PickTransactionId, cancellationToken)
            ?? throw new Exception($"PickTransaction with ID {request.PickTransactionId} not found.");

        if (pickTransaction.Status != PickStatus.Planned)
        {
            throw new InvalidOperationException("Only planned pick items can be deleted.");
        }

        pickRepository.Remove(pickTransaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
