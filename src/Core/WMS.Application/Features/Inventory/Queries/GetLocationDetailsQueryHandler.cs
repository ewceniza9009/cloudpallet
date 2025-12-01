using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Inventory.Queries;

public class GetLocationDetailsQueryHandler(IWarehouseRepository warehouseRepository)
    : IRequestHandler<GetLocationDetailsQuery, LocationDetailsDto?>
{
    public async Task<LocationDetailsDto?> Handle(GetLocationDetailsQuery request, CancellationToken cancellationToken)
    {
        return await warehouseRepository.GetLocationDetailsAsync(request.LocationId, cancellationToken);
    }
}
