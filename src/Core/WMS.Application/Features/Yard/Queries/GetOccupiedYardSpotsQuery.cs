using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Yard.Queries;

public record OccupiedYardSpotDto(
    Guid YardSpotId,
    string SpotNumber,
    string LicensePlate,
    Guid TruckId,
    Guid AppointmentId,
    DateTime OccupiedSince);

public record GetOccupiedYardSpotsQuery(Guid WarehouseId) : IRequest<IEnumerable<OccupiedYardSpotDto>>;

public class GetOccupiedYardSpotsQueryHandler(IWarehouseRepository warehouseRepository)  
    : IRequestHandler<GetOccupiedYardSpotsQuery, IEnumerable<OccupiedYardSpotDto>>
{
    public async Task<IEnumerable<OccupiedYardSpotDto>> Handle(GetOccupiedYardSpotsQuery request, CancellationToken cancellationToken)
    {
        return await warehouseRepository.GetOccupiedYardSpotsAsync(request.WarehouseId, cancellationToken);  
    }
}