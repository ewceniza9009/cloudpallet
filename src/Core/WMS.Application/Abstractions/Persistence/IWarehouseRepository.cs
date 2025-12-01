using WMS.Domain.Aggregates.Warehouse;
using WMS.Application.Features.Yard.Queries;
using WMS.Application.Features.Warehouse.Queries;
using WMS.Domain.Entities.Transaction;
using WMS.Application.Features.Inventory.Queries;

namespace WMS.Application.Abstractions.Persistence;

public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdWithYardSpotsAsync(Guid id, CancellationToken cancellationToken);
    Task<YardSpot?> GetYardSpotByIdAsync(Guid yardSpotId, CancellationToken cancellationToken);
    Task<Dock?> GetDockByIdAsync(Guid dockId, CancellationToken cancellationToken);
    Task<IEnumerable<OccupiedYardSpotDto>> GetOccupiedYardSpotsAsync(Guid warehouseId, CancellationToken cancellationToken);
    Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken);
    Task<LocationOverviewDto?> GetLocationOverviewAsync(Guid warehouseId, CancellationToken cancellationToken);
    Task<IEnumerable<Pallet>> GetPalletsInStagingAsync(Guid warehouseId, CancellationToken cancellationToken);
    Task<IEnumerable<Pallet>> GetStoredPalletsAsync(Guid warehouseId, CancellationToken cancellationToken);
    Task<Room?> GetRoomByLocationIdAsync(Guid locationId, CancellationToken cancellationToken);
    Task<IEnumerable<RoomWithPalletsDto>> GetStoredPalletsByRoomAsync(Guid warehouseId, CancellationToken cancellationToken);

    Task<IEnumerable<StoredPalletSearchResultDto>> SearchStoredPalletsAsync(
        Guid warehouseId,
        Guid? accountId,
        Guid? materialId,
        string? barcodeQuery,
        CancellationToken cancellationToken);

    Task<LocationDetailsDto?> GetLocationDetailsAsync(Guid locationId, CancellationToken cancellationToken);
}