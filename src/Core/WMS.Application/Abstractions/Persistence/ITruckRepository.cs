using WMS.Domain.Entities;

namespace WMS.Application.Abstractions.Persistence;

public interface ITruckRepository
{
    Task<Truck?> GetByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken);
    Task AddAsync(Truck truck, CancellationToken cancellationToken);
    Task<Truck?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<Truck>> GetByCarrierIdAsync(Guid carrierId, CancellationToken cancellationToken);
    void Remove(Truck truck);
}