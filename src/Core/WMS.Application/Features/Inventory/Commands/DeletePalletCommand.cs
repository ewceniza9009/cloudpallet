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
        var receiving = await receivingRepository.GetByIdAsync(request.ReceivingId, cancellationToken)
            ?? throw new InvalidOperationException($"Receiving session {request.ReceivingId} not found.");

        var pallet = await receivingRepository.GetPalletWithLinesByIdAsync(request.PalletId, cancellationToken)
            ?? throw new InvalidOperationException($"Pallet {request.PalletId} not found.");

        // Validate that the pallet belongs to this receiving session
        if (pallet.ReceivingId != request.ReceivingId)
        {
            throw new InvalidOperationException("Pallet does not belong to this receiving session.");
        }

        // Remove inventory records first
        await inventoryRepository.RemoveByPalletIdAsync(request.PalletId, cancellationToken);

        // Decrement the pallet count BEFORE removing the pallet
        receiving.DecrementPalletCount();

        // Remove the pallet
        receivingRepository.RemovePallet(pallet);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}