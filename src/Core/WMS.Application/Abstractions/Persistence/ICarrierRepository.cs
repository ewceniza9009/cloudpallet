using WMS.Domain.Entities;

namespace WMS.Application.Abstractions.Persistence;

public interface ICarrierRepository
{
    Task<Carrier> GetOrCreateDefaultCarrierAsync(CancellationToken cancellationToken);
    Task AddAsync(Carrier carrier, CancellationToken cancellationToken);
    Task<Carrier?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<Carrier>> GetAllWithTrucksAsync(CancellationToken cancellationToken);
    void Remove(Carrier carrier);
}