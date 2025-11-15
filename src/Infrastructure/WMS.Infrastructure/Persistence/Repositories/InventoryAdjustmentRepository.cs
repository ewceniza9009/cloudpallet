using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Persistence.Repositories;

public class InventoryAdjustmentRepository(WmsDbContext context) : IInventoryAdjustmentRepository
{
    public Task AddAsync(InventoryAdjustment adjustment, CancellationToken cancellationToken)
    {
        context.Set<InventoryAdjustment>().Add(adjustment);
        return Task.CompletedTask;
    }
}