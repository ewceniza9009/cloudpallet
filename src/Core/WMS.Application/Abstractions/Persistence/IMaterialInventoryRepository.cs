using WMS.Domain.Entities;

namespace WMS.Application.Abstractions.Persistence;

public interface IMaterialInventoryRepository
{
    Task AddAsync(MaterialInventory inventory, CancellationToken cancellationToken);
    Task<MaterialInventory?> GetByIdAsync(Guid materialInventoryId, CancellationToken cancellationToken);
    Task<MaterialInventory?> GetByIdWithTrackingAsync(Guid materialInventoryId, CancellationToken cancellationToken);
    Task<MaterialInventory?> GetByBarcodeWithTrackingAsync(string barcode, CancellationToken cancellationToken);
    Task<IEnumerable<MaterialInventory>> FindFifoInventoryForMaterialAsync(Guid materialId, CancellationToken cancellationToken);
    Task<IEnumerable<MaterialInventory>> GetByPalletIdAsync(Guid palletId, CancellationToken cancellationToken);
    Task<IEnumerable<MaterialInventory>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken);
    Task RemoveByPalletIdAsync(Guid palletId, CancellationToken cancellationToken);
    Task<MaterialInventory?> GetByPalletLineIdAsync(Guid palletLineId, CancellationToken cancellationToken);
}