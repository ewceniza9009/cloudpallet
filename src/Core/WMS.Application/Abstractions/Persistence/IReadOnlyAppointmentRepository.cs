using WMS.Domain.Aggregates.DockAppointment;

namespace WMS.Application.Abstractions.Persistence;

public interface IReadOnlyAppointmentRepository
{
    Task<IEnumerable<DockAppointment>> GetAppointmentsForDockOnDateAsync(Guid dockId, DateTime date, CancellationToken cancellationToken);
    Task<IEnumerable<DockAppointmentDto>> GetAppointmentsWithTrucksForDockInRangeAsync(Guid dockId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
    Task<IEnumerable<DockAppointment>> GetAppointmentsByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken);
    Task<IEnumerable<DockAppointment>> GetAppointmentsForDateRangeAsync(Guid warehouseId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
    Task<Guid?> GetWarehouseIdForAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken);
}