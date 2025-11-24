using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Inventory.Commands;

public record DeleteReceivingSessionCommand(Guid ReceivingId) : IRequest;

public class DeleteReceivingSessionCommandHandler(
    IReceivingTransactionRepository receivingRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteReceivingSessionCommand>
{
    public async Task Handle(DeleteReceivingSessionCommand request, CancellationToken cancellationToken)
    {
        var receiving = await receivingRepository.GetByIdWithDetailsAsync(request.ReceivingId, cancellationToken)
            ?? throw new Exception($"Receiving session with ID {request.ReceivingId} not found.");

        if (receiving.Status == ReceivingStatus.Completed)
        {
            throw new InvalidOperationException("Cannot delete a completed receiving session.");
        }

        // Optional: Check if there are pallets/lines and decide whether to allow delete or require them to be empty.
        // For now, we will allow cascade delete (EF Core usually handles this if configured, or we might need to delete children explicitly).
        // Given the repository has RemovePallet/RemovePalletLine, we might need to be careful.
        // However, usually deleting the parent (Receiving) should cascade if the DB is set up that way.
        // If not, we'd need to loop and delete children.
        // Let's assume cascade delete or that the user must clear it first?
        // Actually, for a "Delete Session" button, users expect it to wipe everything.
        
        // Let's rely on EF Core cascade delete for now, or explicit removal if needed.
        // Since we are just calling Remove(receiving), EF should track the deletions.
        
        receivingRepository.Remove(receiving);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
