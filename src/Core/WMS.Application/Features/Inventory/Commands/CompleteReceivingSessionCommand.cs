using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Inventory.Commands;

public record CompleteReceivingSessionCommand(Guid ReceivingId) : IRequest;

public class CompleteReceivingSessionCommandHandler(
    IReceivingTransactionRepository receivingRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteReceivingSessionCommand>
{
    public async Task Handle(CompleteReceivingSessionCommand request, CancellationToken cancellationToken)
    {
        var receiving = await receivingRepository.GetByIdAsync(request.ReceivingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Receiving session with ID {request.ReceivingId} not found.");

        // Call the domain method to update the status
        receiving.Complete();

        // No need to call an update method on the repository since EF Core tracks the change
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}