using WMS.Domain.Aggregates.Cargo;

namespace WMS.Application.Abstractions.Persistence;

public interface ICargoManifestRepository
{
    Task AddAsync(CargoManifest manifest, CancellationToken cancellationToken);
    Task<CargoManifest?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken);
}