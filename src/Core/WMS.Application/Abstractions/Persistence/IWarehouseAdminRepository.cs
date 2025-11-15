using AppPagedResult = WMS.Application.Common.Models.PagedResult<WMS.Domain.Aggregates.Warehouse.Location>;
using WMS.Application.Features.LocationSetup.Queries;
using WMS.Domain.Aggregates.Warehouse;

namespace WMS.Application.Abstractions.Persistence;

public interface IWarehouseAdminRepository
{
    Task<IEnumerable<Warehouse>> GetAllAsync(CancellationToken cancellationToken);
    Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken, bool? withTracking = false);     
    Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken);
    void Remove(Warehouse warehouse);

    Task<IEnumerable<Room>> GetAllRoomsAsync(CancellationToken cancellationToken);
    Task<Room?> GetRoomByIdAsync(Guid id, CancellationToken cancellationToken);     
    Task<bool> DoesLocationExist(Guid roomId, string bay, int row, int col, int level, CancellationToken cancellationToken);
    Task<AppPagedResult> GetLocationsForRoomAsync(GetLocationsForRoomQuery query, CancellationToken cancellationToken);
    Task<Location?> GetLocationByIdAsync(Guid locationId, CancellationToken cancellationToken);     
    void RemoveLocation(Location location);

    void AddRoom(Room room);

    Task<Warehouse?> GetByIdWithDocksAndYardSpotsAsync(Guid warehouseId, CancellationToken cancellationToken);
    Task<Dock?> GetDockByIdAsync(Guid dockId, CancellationToken cancellationToken);
    void RemoveDock(Dock dock);
    Task<YardSpot?> GetYardSpotByIdAsync(Guid yardSpotId, CancellationToken cancellationToken);
    void RemoveYardSpot(YardSpot yardSpot);
}