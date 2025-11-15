using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities.Transaction;

namespace WMS.Application.Features.Inventory.Commands;

public record AddLineRequest(Guid MaterialId);

public record AddLineToPalletCommand(
    Guid ReceivingId,
    Guid PalletId,
    Guid MaterialId) : IRequest;

public class AddLineToPalletCommandHandler(
    IReceivingTransactionRepository receivingRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddLineToPalletCommand>
{
    public async Task Handle(AddLineToPalletCommand request, CancellationToken cancellationToken)
    {
        var pallet = await receivingRepository.GetPalletWithLinesByIdAsync(request.PalletId, cancellationToken)
            ?? throw new KeyNotFoundException($"Pallet with ID {request.PalletId} not found.");

        var receiving = await receivingRepository.GetByIdAsync(request.ReceivingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Receiving session with ID {request.ReceivingId} not found.");

        if (pallet.Lines.Any(l => l.MaterialId == request.MaterialId))
        {
            return;         
        }

        var newPalletLine = PalletLine.Create(
            pallet.Id,
            request.MaterialId,
            0,
            0,
            "PENDING",
            DateTime.UtcNow,
            null,
            receiving.AccountId!.Value
        );

        await receivingRepository.AddPalletLineAsync(newPalletLine, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}