using WMS.Domain.Entities.Transaction;

namespace WMS.Application.Abstractions.Persistence;

public interface IWithdrawalTransactionRepository
{
    Task AddAsync(WithdrawalTransaction transaction, CancellationToken cancellationToken);
    Task<IEnumerable<WithdrawalTransaction>> GetForAccountByPeriodAsync(Guid accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
    Task<WithdrawalTransaction?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken);    
}