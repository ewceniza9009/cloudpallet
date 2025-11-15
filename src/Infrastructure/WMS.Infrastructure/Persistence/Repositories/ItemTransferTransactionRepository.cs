using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Persistence.Repositories;

public class ItemTransferTransactionRepository(WmsDbContext context) : IItemTransferTransactionRepository
{
    public Task AddAsync(ItemTransferTransaction transaction, CancellationToken cancellationToken)
    {
        context.Set<ItemTransferTransaction>().Add(transaction);
        return Task.CompletedTask;
    }
}