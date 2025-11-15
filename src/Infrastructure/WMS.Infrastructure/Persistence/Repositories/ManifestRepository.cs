using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Aggregates.Cargo;

namespace WMS.Infrastructure.Persistence.Repositories;

public class CargoManifestRepository(WmsDbContext context) : ICargoManifestRepository
{
    public Task AddAsync(CargoManifest manifest, CancellationToken cancellationToken)
    {
        context.Set<CargoManifest>().Add(manifest);
        return Task.CompletedTask;
    }

    public async Task<CargoManifest?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        return await context.Set<CargoManifest>()
            .Include(cm => cm.Lines)
            .FirstOrDefaultAsync(cm => cm.DockAppointmentId == appointmentId, cancellationToken);
    }
}