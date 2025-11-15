using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Persistence.Repositories;

public class MaterialInventoryRepository(WmsDbContext context) : IMaterialInventoryRepository
{
    public Task AddAsync(MaterialInventory inventory, CancellationToken cancellationToken)
    {
        context.MaterialInventories.Add(inventory);
        return Task.CompletedTask;
    }    

    public Task<MaterialInventory?> GetByIdAsync(Guid materialInventoryId, CancellationToken cancellationToken)
    {
        return context.MaterialInventories
            .AsNoTracking()
            .FirstOrDefaultAsync(inv => inv.Id == materialInventoryId, cancellationToken);
    }

    public Task<MaterialInventory?> GetByIdWithTrackingAsync(Guid materialInventoryId, CancellationToken cancellationToken)
    {
        return context.MaterialInventories
            .Include(mi => mi.Pallet).ThenInclude(p => p.Receiving) // Include related data needed by handler
            .FirstOrDefaultAsync(inv => inv.Id == materialInventoryId, cancellationToken);
    }

    public Task<MaterialInventory?> GetByBarcodeWithTrackingAsync(string barcode, CancellationToken cancellationToken)
    {
        // Use AsTracking() because the command handler needs to modify this entity (AdjustForWeighedPick)
        return context.MaterialInventories
            .AsTracking()
            .Include(mi => mi.Pallet).ThenInclude(p => p.Receiving)
            .Include(mi => mi.Location)
            .FirstOrDefaultAsync(inv => inv.Barcode == barcode, cancellationToken);
    }

    public async Task<IEnumerable<MaterialInventory>> FindFifoInventoryForMaterialAsync(Guid materialId, CancellationToken cancellationToken)
    {
        return await context.MaterialInventories
            .Include(inv => inv.Location)
            .Where(inv =>
                inv.MaterialId == materialId &&
                inv.Quantity > 0 &&
                inv.Location.ZoneType == LocationType.Storage)
            .OrderBy(inv => inv.ExpiryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MaterialInventory>> GetByPalletIdAsync(Guid palletId, CancellationToken cancellationToken)
    {
        return await context.MaterialInventories
            .Where(mi => mi.PalletId == palletId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MaterialInventory>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken)
    {
        return await context.MaterialInventories
            .AsNoTracking()           
            .Where(mi => mi.AccountId == accountId)
            .ToListAsync(cancellationToken);
    }

    public async Task RemoveByPalletIdAsync(Guid palletId, CancellationToken cancellationToken)
    {
        var inventoryItems = await context.MaterialInventories
            .Where(mi => mi.PalletId == palletId)
            .ToListAsync(cancellationToken);

        if (inventoryItems.Any())
        {
            context.MaterialInventories.RemoveRange(inventoryItems);
        }
    }

    public async Task<MaterialInventory?> GetByPalletLineIdAsync(Guid palletLineId, CancellationToken cancellationToken)
    {
        return await context.MaterialInventories
            .FirstOrDefaultAsync(mi => mi.PalletLineId == palletLineId, cancellationToken);
    }
}