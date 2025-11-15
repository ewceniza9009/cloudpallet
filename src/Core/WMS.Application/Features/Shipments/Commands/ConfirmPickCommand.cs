using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Shipments.Commands;

public record ConfirmPickCommand(Guid PickTransactionId, PickStatus NewStatus, Guid UserId) : IRequest;

public class ConfirmPickCommandHandler(
    IPickTransactionRepository pickRepository,
    IMaterialInventoryRepository inventoryRepository, // ADD THIS
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmPickCommand>
{
    public async Task Handle(ConfirmPickCommand request, CancellationToken cancellationToken)
    {
        var pickTransaction = await pickRepository.GetByIdWithDetailsAsync(request.PickTransactionId, cancellationToken)
            ?? throw new Exception($"PickTransaction with ID {request.PickTransactionId} not found.");

        if (pickTransaction.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("User is not authorized to confirm this pick.");
        }

        if (pickTransaction.Status != PickStatus.Planned)
        {
            throw new InvalidOperationException("This pick has already been completed.");
        }

        // THIS IS THE FIX: Deduct inventory at the moment of confirmation
        if (request.NewStatus == PickStatus.Confirmed)
        {
            pickTransaction.MaterialInventory.AdjustQuantity(-pickTransaction.Quantity);
        }

        pickTransaction.ConfirmPick(request.NewStatus, 0); // Pass 0 for weight since it's a manual confirmation

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}