using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Inventory.Commands;

public record ResetPalletLineStatusCommand(Guid PalletLineId) : IRequest;

public class ResetPalletLineStatusCommandHandler(
    IReceivingTransactionRepository receivingRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ResetPalletLineStatusCommand>
{
    public async Task Handle(ResetPalletLineStatusCommand request, CancellationToken cancellationToken)
    {
        var palletLine = await receivingRepository.GetPalletLineByIdAsync(request.PalletLineId, cancellationToken)
            ?? throw new KeyNotFoundException($"Pallet line with ID {request.PalletLineId} not found.");

        palletLine.Reset();

        await receivingRepository.UpdatePalletLineAsync(palletLine, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}