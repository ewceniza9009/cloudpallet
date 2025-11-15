using WMS.Domain.Entities.Transaction;

namespace WMS.Application.Abstractions.Persistence;

public interface IPutawayTransactionRepository
{
    Task AddAsync(PutawayTransaction transaction, CancellationToken cancellationToken);
    Task<bool> HasBeenPutAwayAsync(Guid palletId, CancellationToken cancellationToken);
}