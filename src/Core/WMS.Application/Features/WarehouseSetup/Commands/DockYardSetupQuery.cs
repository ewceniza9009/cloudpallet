using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;

namespace WMS.Application.Features.WarehouseSetup.Queries;

public record DockSetupDto(Guid Id, string Name, DockType Type);
public record YardSpotSetupDto(Guid Id, string SpotNumber, bool IsActive);

public record DockYardSetupDto(
    List<DockSetupDto> Docks,
    List<YardSpotSetupDto> YardSpots);

public record GetDockYardSetupQuery(Guid WarehouseId) : IRequest<DockYardSetupDto>;

public class GetDockYardSetupQueryHandler(IWarehouseAdminRepository warehouseAdminRepository)
    : IRequestHandler<GetDockYardSetupQuery, DockYardSetupDto>
{
    public async Task<DockYardSetupDto> Handle(GetDockYardSetupQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouseAdminRepository.GetByIdWithDocksAndYardSpotsAsync(request.WarehouseId, cancellationToken)
            ?? throw new KeyNotFoundException("Warehouse not found.");

        var docks = warehouse.Docks
            .Select(d => new DockSetupDto(d.Id, d.Name, d.Type))
            .OrderBy(d => d.Name)
            .ToList();

        var yardSpots = warehouse.YardSpots
            .Select(ys => new YardSpotSetupDto(ys.Id, ys.SpotNumber, ys.IsActive))
            .OrderBy(ys => ys.SpotNumber)
            .ToList();

        return new DockYardSetupDto(docks, yardSpots);
    }
}