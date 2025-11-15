using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record UpdateYardSpotCommand(Guid YardSpotId, string SpotNumber, bool IsActive) : IRequest;

public class UpdateYardSpotCommandHandler(IWarehouseAdminRepository warehouseAdminRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateYardSpotCommand>
{
    public async Task Handle(UpdateYardSpotCommand request, CancellationToken cancellationToken)
    {
        var spot = await warehouseAdminRepository.GetYardSpotByIdAsync(request.YardSpotId, cancellationToken)
            ?? throw new KeyNotFoundException("Yard Spot not found.");

        spot.Update(request.SpotNumber, request.IsActive); // Assumes YardSpot.cs has an Update method
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}