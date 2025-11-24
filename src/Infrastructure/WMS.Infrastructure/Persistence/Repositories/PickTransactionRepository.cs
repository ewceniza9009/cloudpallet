using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities.Transaction;
using WMS.Application.Features.Shipments.Queries;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Persistence.Repositories;

public class PickTransactionRepository(WmsDbContext context) : IPickTransactionRepository
{
    public Task AddAsync(PickTransaction transaction, CancellationToken cancellationToken)
    {
        context.PickTransactions.Add(transaction);
        return Task.CompletedTask;
    }

    public async Task<PickTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.PickTransactions.FindAsync([id], cancellationToken);
    }

    public async Task<IEnumerable<PickTransaction>> GetByIdsWithDetailsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        return await context.PickTransactions
            .Include(p => p.MaterialInventory)
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PickTransaction>> GetPendingPicksWithDetailsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await context.PickTransactions
            .Where(p => p.UserId == userId && p.Status == PickStatus.Planned)
            .Include(p => p.MaterialInventory.Material)
            .Include(p => p.MaterialInventory.Location)
            .Include(p => p.Account)          
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<PickTransaction?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.PickTransactions
            .Include(p => p.MaterialInventory)
            .ThenInclude(mi => mi.Location)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ShippableGroupDto>> GetConfirmedPicksGroupedByAccountAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        var storageLocationIds = await context.Warehouses
            .Where(w => w.Id == warehouseId)
            .SelectMany(w => w.Rooms)
            .SelectMany(r => r.Locations)
            .Where(l => l.ZoneType == LocationType.Storage || l.ZoneType == LocationType.Picking)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        var confirmedPicks = await context.PickTransactions
            .AsNoTracking()
            .Include(p => p.Account)
            .Where(p => p.Status == PickStatus.Confirmed &&
                        !p.WithdrawalTransactions.Any() &&
                        context.MaterialInventories.Any(mi => mi.Id == p.InventoryId && storageLocationIds.Contains(mi.LocationId)))
            .ToListAsync(cancellationToken);

        var groupedByAccount = confirmedPicks
            .GroupBy(p => p.AccountId)
            .Select(g => new ShippableGroupDto
            {
                AccountId = g.Key,
                AccountName = g.First().Account.Name,
                ItemCount = g.Count(),
                TotalQuantity = g.Sum(p => p.Quantity),
                PickTransactionIds = g.Select(p => p.Id).ToList()
            })
            .ToList();

        return groupedByAccount;
    }

    public async Task<IEnumerable<PickTransaction>> GetForAccountByPeriodAsync(Guid accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await context.PickTransactions
            .AsNoTracking()
            .Where(p => p.AccountId == accountId && p.Timestamp >= startDate && p.Timestamp < endDate)
            .ToListAsync(cancellationToken);
    }

    public void Remove(PickTransaction transaction)
    {
        context.PickTransactions.Remove(transaction);
    }
}