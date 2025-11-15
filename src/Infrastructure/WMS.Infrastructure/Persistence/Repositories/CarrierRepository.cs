using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Repositories;

public class CarrierRepository(WmsDbContext context) : ICarrierRepository
{
    private const string DefaultCarrierName = "Unspecified Carrier";
    private const string DefaultCarrierScac = "UNSP";

    public async Task<Carrier> GetOrCreateDefaultCarrierAsync(CancellationToken cancellationToken)
    {
        var defaultCarrier = await context.Carriers.FirstOrDefaultAsync(c => c.Name == DefaultCarrierName, cancellationToken);

        if (defaultCarrier is null)
        {
            defaultCarrier = Carrier.Create(DefaultCarrierName, DefaultCarrierScac);
            await AddAsync(defaultCarrier, cancellationToken);
        }

        return defaultCarrier;
    }

    public Task AddAsync(Carrier carrier, CancellationToken cancellationToken)
    {
        context.Carriers.Add(carrier);
        return Task.CompletedTask;
    }

    public async Task<Carrier?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Carriers
            .Include(c => c.Address)      
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Carrier>> GetAllWithTrucksAsync(CancellationToken cancellationToken)
    {
        return await context.Carriers
            .AsNoTracking()
            .Include(c => c.Trucks)     
            .Include(c => c.Address)      
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public void Remove(Carrier carrier)
    {
        context.Carriers.Remove(carrier);
    }
}