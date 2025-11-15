using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Persistence.Repositories;

public class TransferTransactionRepository(WmsDbContext context) : ITransferTransactionRepository
{
    public Task AddAsync(TransferTransaction transaction, CancellationToken cancellationToken)
    {
        context.Set<TransferTransaction>().Add(transaction);
        return Task.CompletedTask;
    }
}