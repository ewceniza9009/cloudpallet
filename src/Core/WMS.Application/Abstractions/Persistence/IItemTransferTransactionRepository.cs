using WMS.Domain.Entities.Transaction;

namespace WMS.Application.Abstractions.Persistence;

public interface IItemTransferTransactionRepository
{
    Task AddAsync(ItemTransferTransaction transaction, CancellationToken cancellationToken);
}