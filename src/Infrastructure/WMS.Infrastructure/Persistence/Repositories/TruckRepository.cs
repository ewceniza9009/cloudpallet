using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Repositories;

public class TruckRepository(WmsDbContext context) : ITruckRepository
{
    public async Task<Truck?> GetByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken)
    {
        return await context.Trucks
            .FirstOrDefaultAsync(t => t.LicensePlate == licensePlate, cancellationToken);
    }

    public Task AddAsync(Truck truck, CancellationToken cancellationToken)
    {
        context.Trucks.Add(truck);
        return Task.CompletedTask;
    }

    public async Task<Truck?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Trucks.FindAsync([id], cancellationToken);
    }

    public async Task<IEnumerable<Truck>> GetByCarrierIdAsync(Guid carrierId, CancellationToken cancellationToken)
    {
        return await context.Trucks
            .AsNoTracking()
            .Where(t => t.CarrierId == carrierId)
            .OrderBy(t => t.LicensePlate)
            .ToListAsync(cancellationToken);
    }

    public void Remove(Truck truck)
    {
        context.Trucks.Remove(truck);
    }
}