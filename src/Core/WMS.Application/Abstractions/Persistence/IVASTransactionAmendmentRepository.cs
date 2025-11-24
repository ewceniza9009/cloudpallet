using WMS.Domain.Entities.Transaction;

namespace WMS.Application.Abstractions.Persistence;

public interface IVASTransactionAmendmentRepository
{
    Task AddAsync(VASTransactionAmendment amendment, CancellationToken cancellationToken);
    Task<IEnumerable<VASTransactionAmendment>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken);
}
