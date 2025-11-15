namespace WMS.Application.Abstractions.Persistence;
using WMS.Domain.Aggregates.DockAppointment;

public interface IDockAppointmentRepository
{
    Task AddAsync(DockAppointment appointment, CancellationToken cancellationToken);
    Task<DockAppointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> DoesAppointmentOverlapAsync(Guid dockId, DateTime start, DateTime end, CancellationToken cancellationToken);

    Task<IEnumerable<DockAppointment>> GetAppointmentsForTruckByDateAsync(Guid truckId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
}