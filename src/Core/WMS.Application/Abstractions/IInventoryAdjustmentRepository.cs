using WMS.Domain.Entities.Transaction;

namespace WMS.Application.Abstractions.Persistence;

public interface IInventoryAdjustmentRepository
{
    Task AddAsync(InventoryAdjustment adjustment, CancellationToken cancellationToken);
}