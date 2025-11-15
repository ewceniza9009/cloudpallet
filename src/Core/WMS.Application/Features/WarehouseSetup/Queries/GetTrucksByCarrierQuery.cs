using MediatR;
using Microsoft.EntityFrameworkCore; // For IQueryable/ToListAsync
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;

namespace WMS.Application.Features.WarehouseSetup.Queries;

public record TruckDto(
    Guid Id,
    Guid CarrierId,
    string LicensePlate,
    string Model,
    decimal CapacityWeight,
    decimal CapacityVolume,
    bool IsActive);

public record GetTrucksByCarrierQuery(Guid CarrierId) : IRequest<IEnumerable<TruckDto>>;

public class GetTrucksByCarrierQueryHandler(ITruckRepository truckRepository)
    : IRequestHandler<GetTrucksByCarrierQuery, IEnumerable<TruckDto>>
{
    public async Task<IEnumerable<TruckDto>> Handle(GetTrucksByCarrierQuery request, CancellationToken cancellationToken)
    {
        var trucks = await truckRepository.GetByCarrierIdAsync(request.CarrierId, cancellationToken);

        return trucks.Select(t => new TruckDto(
            t.Id,
            t.CarrierId,
            t.LicensePlate,
            t.Model,
            t.CapacityWeight,
            t.CapacityVolume,
            t.IsActive
        )).OrderBy(t => t.LicensePlate);
    }
}