using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Inventory.Commands;

public record DeletePalletLineCommand(Guid ReceivingId, Guid PalletId, Guid PalletLineId) : IRequest;

public class DeletePalletLineCommandHandler(
    IReceivingTransactionRepository receivingRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePalletLineCommand>
{
    public async Task Handle(DeletePalletLineCommand request, CancellationToken cancellationToken)
    {
        var pallet = await receivingRepository.GetPalletWithLinesByIdAsync(request.PalletId, cancellationToken)
            ?? throw new KeyNotFoundException($"Pallet with ID {request.PalletId} not found.");

        var lineToRemove = pallet.Lines.FirstOrDefault(l => l.Id == request.PalletLineId)
            ?? throw new KeyNotFoundException($"Pallet line with ID {request.PalletLineId} not found on pallet.");

        receivingRepository.RemovePalletLine(lineToRemove);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}