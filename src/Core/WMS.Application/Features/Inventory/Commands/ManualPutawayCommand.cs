using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities.Transaction;

public record ManualPutawayCommand(Guid PalletId, Guid DestinationLocationId, Guid UserId) : IRequest<Guid>;

public class ManualPutawayCommandHandler(
  IPutawayTransactionRepository putawayRepo,
  IMaterialInventoryRepository inventoryRepo,
  ILocationRepository locationRepository,
  IUnitOfWork unitOfWork) : IRequestHandler<ManualPutawayCommand, Guid>
{
    public async Task<Guid> Handle(ManualPutawayCommand request, CancellationToken cancellationToken)
    {
        if (await putawayRepo.HasBeenPutAwayAsync(request.PalletId, cancellationToken))
        {
            throw new InvalidOperationException($"Pallet with ID {request.PalletId} has already been put away.");
        }

        var allPalletInventory = (await inventoryRepo.GetByPalletIdAsync(request.PalletId, cancellationToken)).ToList();

        if (!allPalletInventory.Any())
        {
            throw new InvalidOperationException($"No inventory items found for Pallet ID {request.PalletId}.");
        }

        var firstInventory = allPalletInventory.First();

        var sourceLocation = await locationRepository.GetByIdAsync(firstInventory.LocationId, cancellationToken)
          ?? throw new InvalidOperationException($"Source location {firstInventory.LocationId} not found.");

        var destinationLocation = await locationRepository.GetByIdAsync(request.DestinationLocationId, cancellationToken)
          ?? throw new InvalidOperationException($"Destination location {request.DestinationLocationId} not found.");

        sourceLocation.MarkAsEmpty();
        destinationLocation.MarkAsFilled();

        foreach (var item in allPalletInventory)
        {
            item.MoveToLocation(request.DestinationLocationId);
        }

        var putawayTx = PutawayTransaction.Create(request.PalletId, request.DestinationLocationId, request.UserId);
        await putawayRepo.AddAsync(putawayTx, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return putawayTx.Id;
    }
}