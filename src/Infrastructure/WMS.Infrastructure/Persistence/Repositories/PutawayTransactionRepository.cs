using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Persistence.Repositories;

public class PutawayTransactionRepository(WmsDbContext context) : IPutawayTransactionRepository
{
    public Task AddAsync(PutawayTransaction transaction, CancellationToken cancellationToken)
    {
        context.Set<PutawayTransaction>().Add(transaction);
        return Task.CompletedTask;
    }

    public async Task<bool> HasBeenPutAwayAsync(Guid palletId, CancellationToken cancellationToken)
    {
        return await context.Set<PutawayTransaction>()
            .AnyAsync(pt => pt.PalletId == palletId, cancellationToken);
    }
}