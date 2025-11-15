using WMS.Domain.Aggregates.Warehouse;
using WMS.Domain.Enums;

namespace WMS.Application.Abstractions.Persistence;

public interface ILocationRepository
{
    Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken);    
    Task<Location?> GetDefaultReceivingLocationForWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken);
    Task<Location?> FindBestAvailableLocationAsync(ServiceType tempZone, CancellationToken cancellationToken);
}