using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Shipments.Commands;

public record ConfirmPickByScanCommand(
    Guid PickTransactionId,
    string ScannedLocationCode,
    string ScannedLpn,
    decimal ActualWeight,
    Guid UserId) : IRequest;

public class ConfirmPickByScanCommandHandler(IPickTransactionRepository pickRepo, IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmPickByScanCommand>
{
    public async Task Handle(ConfirmPickByScanCommand request, CancellationToken cancellationToken)
    {
        var pickTransaction = await pickRepo.GetByIdWithDetailsAsync(request.PickTransactionId, cancellationToken)
            ?? throw new Exception($"PickTransaction with ID {request.PickTransactionId} not found.");

        if (pickTransaction.Status != PickStatus.Planned)
        {
            throw new InvalidOperationException("This pick has already been completed or actioned.");
        }

        if (pickTransaction.MaterialInventory.Location.Barcode != request.ScannedLocationCode)
        {
            throw new InvalidOperationException("Verification failed: Scanned location does not match the required pick location.");
        }

        if (pickTransaction.MaterialInventory.Barcode != request.ScannedLpn)
        {
            throw new InvalidOperationException("Verification failed: Scanned LPN does not match the required inventory.");
        }

        pickTransaction.MaterialInventory.AdjustForWeighedPick(pickTransaction.Quantity, request.ActualWeight);

        pickTransaction.ConfirmPick(PickStatus.Confirmed, request.ActualWeight);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}