using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Warehouse.Queries;

public record LocationDto(Guid Id, string Barcode, int Row, int Column, int Level, decimal CurrentWeight, decimal CapacityWeight, double Utilization, string Status);
public record BayDto(string BayName, decimal CurrentWeight, decimal CapacityWeight, double Utilization, string Status, List<LocationDto> Locations);
public record RoomDto(string RoomName, decimal CurrentWeight, decimal CapacityWeight, double Utilization, List<BayDto> Bays);
public record LocationOverviewDto(List<RoomDto> Rooms);


public record GetLocationOverviewQuery(Guid WarehouseId) : IRequest<LocationOverviewDto?>;

public class GetLocationOverviewQueryHandler(IWarehouseRepository warehouseRepository)
    : IRequestHandler<GetLocationOverviewQuery, LocationOverviewDto?>
{
    public async Task<LocationOverviewDto?> Handle(GetLocationOverviewQuery request, CancellationToken cancellationToken)
    {
        return await warehouseRepository.GetLocationOverviewAsync(request.WarehouseId, cancellationToken);
    }
}