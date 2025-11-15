namespace WMS.Domain.Services;
using Aggregates.DockAppointment;
using Aggregates.Warehouse;

public interface IDockSchedulingService
{
    Task<bool> IsSlotAvailableAsync(Dock dock, DateTime start, DateTime end, CancellationToken cancellationToken = default);
}