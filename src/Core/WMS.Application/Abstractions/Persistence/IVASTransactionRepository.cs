using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;

namespace WMS.Application.Abstractions.Persistence;

public interface IVASTransactionRepository
{
    Task AddAsync(VASTransaction transaction, CancellationToken cancellationToken);
    Task<IEnumerable<VASTransaction>> GetForAccountByPeriodAsync(Guid accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
}