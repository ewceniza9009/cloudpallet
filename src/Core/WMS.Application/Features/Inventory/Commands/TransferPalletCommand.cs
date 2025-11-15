using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities.Transaction;

namespace WMS.Application.Features.Inventory.Commands;

public record TransferPalletCommand(Guid PalletId, Guid SourceLocationId, Guid DestinationLocationId, Guid UserId) : IRequest<Guid>;

public class TransferPalletCommandHandler(
    IMaterialInventoryRepository inventoryRepo,
    ILocationRepository locationRepository,
    ITransferTransactionRepository transferRepo,     
    IUnitOfWork unitOfWork) : IRequestHandler<TransferPalletCommand, Guid>
{
    public async Task<Guid> Handle(TransferPalletCommand request, CancellationToken cancellationToken)
    {
        var allPalletInventory = (await inventoryRepo.GetByPalletIdAsync(request.PalletId, cancellationToken)).ToList();
        if (!allPalletInventory.Any())
        {
            throw new InvalidOperationException($"No inventory items found for Pallet ID {request.PalletId}.");
        }

        var sourceLocation = await locationRepository.GetByIdAsync(request.SourceLocationId, cancellationToken)
            ?? throw new InvalidOperationException($"Source location {request.SourceLocationId} not found.");

        var destinationLocation = await locationRepository.GetByIdAsync(request.DestinationLocationId, cancellationToken)
            ?? throw new InvalidOperationException($"Destination location {request.DestinationLocationId} not found.");

        if (!destinationLocation.IsEmpty)
        {
            throw new InvalidOperationException($"Destination location {destinationLocation.Id} is not empty.");
        }

        sourceLocation.MarkAsEmpty();
        destinationLocation.MarkAsFilled();

        foreach (var item in allPalletInventory)
        {
            item.MoveToLocation(request.DestinationLocationId);
        }

        var transferTx = TransferTransaction.Create(request.PalletId, request.SourceLocationId, request.DestinationLocationId, request.UserId);

        await transferRepo.AddAsync(transferTx, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return transferTx.Id;
    }
}