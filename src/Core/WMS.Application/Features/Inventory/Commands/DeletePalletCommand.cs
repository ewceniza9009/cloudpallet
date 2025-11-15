using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Inventory.Commands;

public record DeletePalletCommand(Guid ReceivingId, Guid PalletId) : IRequest;

public class DeletePalletCommandHandler(
    IReceivingTransactionRepository receivingRepository,
    IMaterialInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePalletCommand>
{
    public async Task Handle(DeletePalletCommand request, CancellationToken cancellationToken)
    {
        var pallet = await receivingRepository.GetPalletWithLinesByIdAsync(request.PalletId, cancellationToken)
            ?? throw new KeyNotFoundException($"Pallet with ID {request.PalletId} not found.");

        if (pallet.ReceivingId != request.ReceivingId)
        {
            throw new InvalidOperationException("Pallet does not belong to the specified receiving session.");
        }

        await inventoryRepository.RemoveByPalletIdAsync(request.PalletId, cancellationToken);

        receivingRepository.RemovePallet(pallet);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}