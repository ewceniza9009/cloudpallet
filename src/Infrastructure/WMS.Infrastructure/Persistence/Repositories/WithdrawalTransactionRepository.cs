using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Persistence.Repositories;

public class WithdrawalTransactionRepository(WmsDbContext context) : IWithdrawalTransactionRepository
{
    public Task AddAsync(WithdrawalTransaction transaction, CancellationToken cancellationToken)
    {
        context.WithdrawalTransactions.Add(transaction);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<WithdrawalTransaction>> GetForAccountByPeriodAsync(Guid accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await context.WithdrawalTransactions
            .AsNoTracking()
            .Where(wt => wt.AccountId == accountId && wt.Timestamp >= startDate && wt.Timestamp < endDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<WithdrawalTransaction?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.WithdrawalTransactions
            .Include(wt => wt.Account)
            .FirstOrDefaultAsync(wt => wt.Id == id, cancellationToken);
    }
}