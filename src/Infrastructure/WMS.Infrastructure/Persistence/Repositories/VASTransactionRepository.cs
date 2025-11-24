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
            .Include("_lines")
            .Where(vt => vt.AccountId == accountId && vt.Timestamp >= startDate && vt.Timestamp < endDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<VASTransaction?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.VASTransactions
            .Include("_lines.Material") // Use string-based include for nested navigation
            .FirstOrDefaultAsync(vt => vt.Id == id, cancellationToken);
    }

    public async Task<VASTransaction?> GetByIdWithLinesAndAmendmentsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.VASTransactions
            .Include("_lines.Material") // String-based include for lines and their materials
            .Include("_amendments.User") // String-based include for amendments and their users
            .FirstOrDefaultAsync(vt => vt.Id == id, cancellationToken);
    }
}