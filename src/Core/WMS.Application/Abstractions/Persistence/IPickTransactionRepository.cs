using WMS.Application.Features.Shipments.Queries;
using WMS.Domain.Entities.Transaction;

namespace WMS.Application.Abstractions.Persistence;

public interface IPickTransactionRepository
{
    Task AddAsync(PickTransaction transaction, CancellationToken cancellationToken);
    Task<PickTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<PickTransaction>> GetByIdsWithDetailsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);
    Task<IEnumerable<PickTransaction>> GetPendingPicksWithDetailsAsync(Guid userId, CancellationToken cancellationToken);
    Task<PickTransaction?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<PickTransaction>> GetForAccountByPeriodAsync(Guid accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
    Task<IEnumerable<ShippableGroupDto>> GetConfirmedPicksGroupedByAccountAsync(Guid warehouseId, CancellationToken cancellationToken);    
    void Remove(PickTransaction transaction);
}