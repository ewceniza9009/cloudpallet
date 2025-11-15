using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record CreateYardSpotCommand(Guid WarehouseId, string SpotNumber) : IRequest<Guid>;

public class CreateYardSpotCommandHandler(IWarehouseAdminRepository warehouseAdminRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateYardSpotCommand, Guid>
{
    public async Task<Guid> Handle(CreateYardSpotCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouseAdminRepository.GetByIdAsync(request.WarehouseId, cancellationToken)
            ?? throw new KeyNotFoundException("Warehouse not found.");

        var newSpot = warehouse.AddYardSpot(request.SpotNumber);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return newSpot.Id;
    }
}