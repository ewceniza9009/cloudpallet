using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Persistence.Repositories;

public class VASTransactionRepository(WmsDbContext context) : IVASTransactionRepository
{
    public Task AddAsync(VASTransaction transaction, CancellationToken cancellationToken)
    {
        context.VASTransactions.Add(transaction);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<VASTransaction>> GetForAccountByPeriodAsync(Guid accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await context.VASTransactions
            .AsNoTracking()
            // --- THIS IS THE FIX ---
            // We Include the private backing field "_lines".
            // EF will load all lines, and the entity's properties
            // (InputLines/OutputLines) will filter them in memory.
            .Include("_lines")
            // --- END FIX ---
            .Where(vt => vt.AccountId == accountId && vt.Timestamp >= startDate && vt.Timestamp < endDate)
            .ToListAsync(cancellationToken);
    }
}