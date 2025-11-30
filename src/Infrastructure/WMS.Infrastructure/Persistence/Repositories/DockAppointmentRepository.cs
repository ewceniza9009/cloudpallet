namespace WMS.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Aggregates.DockAppointment;

public class DockAppointmentRepository(WmsDbContext context) : IDockAppointmentRepository
{
    public Task AddAsync(DockAppointment appointment, CancellationToken cancellationToken)
    {
        context.DockAppointments.Add(appointment);
        return Task.CompletedTask;
    }

    public async Task<DockAppointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.DockAppointments.FindAsync([id], cancellationToken: cancellationToken);
    }

    public Task UpdateAsync(DockAppointment appointment, CancellationToken cancellationToken)
    {
        context.DockAppointments.Update(appointment);
        return Task.CompletedTask;
    }

    public async Task<bool> DoesAppointmentOverlapAsync(Guid dockId, DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        var overlaps = await context.DockAppointments
            .AnyAsync(da => da.DockId == dockId 
                         && da.StartDateTime < end 
                         && da.EndDateTime > start
                         && da.Status != WMS.Domain.Enums.AppointmentStatus.Cancelled, cancellationToken);

        return !overlaps;
    }

    public async Task<IEnumerable<DockAppointment>> GetAppointmentsForTruckByDateAsync(Guid truckId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await context.DockAppointments
            .AsNoTracking()
            .Where(da => da.TruckId == truckId && da.StartDateTime >= startDate && da.EndDateTime < endDate)
            .OrderBy(da => da.StartDateTime)
            .ToListAsync(cancellationToken);
    }
}