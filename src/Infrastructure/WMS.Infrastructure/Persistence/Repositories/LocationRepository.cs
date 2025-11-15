using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Aggregates.Warehouse;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Persistence.Repositories;

public class LocationRepository(WmsDbContext context) : ILocationRepository
{
    public async Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Warehouses
            .SelectMany(w => w.Rooms)
            .SelectMany(r => r.Locations)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<Location?> GetDefaultReceivingLocationForWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        return await context.Warehouses
            .Where(w => w.Id == warehouseId)
            .SelectMany(w => w.Rooms)
            .Where(r => r.TemperatureRange.MaxTemperature <= 15.0m)
            .SelectMany(r => r.Locations)
            .FirstOrDefaultAsync(l => l.ZoneType == LocationType.Staging, cancellationToken);
    }

    public async Task<Location?> FindBestAvailableLocationAsync(ServiceType tempZone, CancellationToken cancellationToken)
    {
        return await context.Warehouses
            .SelectMany(w => w.Rooms)
            .Where(r => r.ServiceType == tempZone)
            .SelectMany(r => r.Locations)
            .Where(l => l.IsEmpty && l.IsActive && l.ZoneType == LocationType.Storage)
            .OrderBy(l => l.Level)
            .ThenBy(l => l.Row)
            .ThenBy(l => l.Column)
            .FirstOrDefaultAsync(cancellationToken);
    }
}