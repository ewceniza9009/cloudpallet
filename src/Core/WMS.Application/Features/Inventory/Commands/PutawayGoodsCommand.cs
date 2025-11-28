using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Services;

namespace WMS.Application.Features.Inventory.Commands;

public record PutawayGoodsCommand(Guid PalletId, Guid UserId) : IRequest<Guid>;

public class PutawayGoodsCommandHandler(
    IPutawayTransactionRepository putawayRepo,
    IMaterialInventoryRepository inventoryRepo,
    ILocationRepository locationRepository,
    IPutawaySuggestionService suggestionService,
    IUnitOfWork unitOfWork) : IRequestHandler<PutawayGoodsCommand, Guid>
{
    public async Task<Guid> Handle(PutawayGoodsCommand request, CancellationToken cancellationToken)
    {
        if (await putawayRepo.HasBeenPutAwayAsync(request.PalletId, cancellationToken))
        {
            return Guid.Empty;
        }

        var allPalletInventory = (await inventoryRepo.GetByPalletIdAsync(request.PalletId, cancellationToken)).ToList();
        if (!allPalletInventory.Any())
        {
            throw new InvalidOperationException($"No inventory items found for Pallet ID {request.PalletId}.");
        }

        var firstInventoryItem = allPalletInventory.First();

        var sourceLocation = await locationRepository.GetByIdAsync(firstInventoryItem.LocationId, cancellationToken)
            ?? throw new InvalidOperationException($"Source location {firstInventoryItem.LocationId} not found.");

        var destinationLocationId = await suggestionService.SuggestLocationAsync(firstInventoryItem.MaterialId, cancellationToken);
        var destinationLocation = await locationRepository.GetByIdAsync(destinationLocationId, cancellationToken)
            ?? throw new InvalidOperationException($"Suggested destination location {destinationLocationId} not found.");

        sourceLocation.MarkAsEmpty();
        destinationLocation.MarkAsFilled();

        foreach (var inventoryItem in allPalletInventory)
        {
            inventoryItem.MoveToLocation(destinationLocationId);
        }

        var putawayTx = PutawayTransaction.Create(request.PalletId, destinationLocationId, request.UserId, firstInventoryItem.BatchNumber, firstInventoryItem.ExpiryDate);
        await putawayRepo.AddAsync(putawayTx, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return putawayTx.Id;
    }
}