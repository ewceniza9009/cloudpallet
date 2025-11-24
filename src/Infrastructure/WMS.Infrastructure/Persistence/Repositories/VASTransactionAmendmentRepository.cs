using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Persistence.Repositories;

public class VASTransactionAmendmentRepository(WmsDbContext context) : IVASTransactionAmendmentRepository
{
    public Task AddAsync(VASTransactionAmendment amendment, CancellationToken cancellationToken)
    {
        context.VASTransactionAmendments.Add(amendment);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<VASTransactionAmendment>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        return await context.VASTransactionAmendments
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.OriginalTransactionId == transactionId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
