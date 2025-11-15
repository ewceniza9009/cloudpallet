using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record DeleteYardSpotCommand(Guid YardSpotId) : IRequest;

public class DeleteYardSpotCommandHandler(IWarehouseAdminRepository warehouseAdminRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteYardSpotCommand>
{
    public async Task Handle(DeleteYardSpotCommand request, CancellationToken cancellationToken)
    {
        var spot = await warehouseAdminRepository.GetYardSpotByIdAsync(request.YardSpotId, cancellationToken)
            ?? throw new KeyNotFoundException("Yard Spot not found.");

        warehouseAdminRepository.RemoveYardSpot(spot);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}