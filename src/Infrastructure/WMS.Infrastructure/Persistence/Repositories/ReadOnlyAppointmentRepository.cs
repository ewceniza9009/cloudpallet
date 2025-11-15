using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.Dock.Appointments.Queries;
using WMS.Domain.Aggregates.DockAppointment;
using WMS.Domain.Enums;
using System;

namespace WMS.Infrastructure.Persistence.Repositories;

public class ReadOnlyAppointmentRepository(WmsDbContext context) : IReadOnlyAppointmentRepository
{
    public async Task<IEnumerable<DockAppointmentDto>> GetAppointmentsWithTrucksForDockInRangeAsync(Guid dockId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await context.DockAppointments
          .AsNoTracking()
          .Include(da => da.Truck)
          .Include(da => da.Dock)
          .Where(da => da.DockId == dockId && da.StartDateTime >= startDate && da.StartDateTime < endDate)
          .OrderBy(da => da.StartDateTime)
          .Select(da => new DockAppointmentDto
          {
              Id = da.Id,
              DockId = da.DockId,
              DockName = da.Dock.Name,
              TruckId = da.TruckId ?? Guid.Empty,
              LicensePlate = da.Truck != null ? da.Truck.LicensePlate : "N/A",

              // FIX: Explicitly specify the Kind as UTC for DTO projection
              StartDateTime = DateTime.SpecifyKind(da.StartDateTime, DateTimeKind.Utc),
              EndDateTime = DateTime.SpecifyKind(da.EndDateTime, DateTimeKind.Utc),

              Status = da.Status,
              YardSpotNumber = context.YardSpots
                      .Where(ys => ys.CurrentTruckId == da.TruckId && ys.Status == YardSpotStatus.Occupied)
                      .Select(ys => ys.SpotNumber)
                      .FirstOrDefault()
          })
          .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DockAppointment>> GetAppointmentsForDockOnDateAsync(Guid dockId, DateTime date, CancellationToken cancellationToken)
    {
        var startDate = date.Date;
        var endDate = startDate.AddDays(1);

        return await context.DockAppointments
          .AsNoTracking()
          .Where(da => da.DockId == dockId && da.StartDateTime >= startDate && da.StartDateTime < endDate)
          .OrderBy(da => da.StartDateTime)
          .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DockAppointment>> GetAppointmentsByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken)
    {
        return await context.DockAppointments
          .AsNoTracking()
          .Include(da => da.Truck)
          .Where(da => da.Truck != null && da.Truck.LicensePlate.Contains(licensePlate))
          .OrderByDescending(da => da.StartDateTime)
          .Take(20)
          .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DockAppointment>> GetAppointmentsForDateRangeAsync(Guid warehouseId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        var dockIds = await context.Warehouses
          .Where(w => w.Id == warehouseId)
          .SelectMany(w => w.Docks.Select(d => d.Id))
          .ToListAsync(cancellationToken);

        if (!dockIds.Any())
        {
            return Enumerable.Empty<DockAppointment>();
        }

        return await context.DockAppointments
          .AsNoTracking()
          .Include(da => da.Truck)
            .ThenInclude(t => t!.Carrier)
          .Include(da => da.Dock)
          .Where(da => dockIds.Contains(da.DockId) && da.StartDateTime >= startDate && da.StartDateTime < endDate)
          .OrderBy(da => da.StartDateTime)
          .ToListAsync(cancellationToken);
    }

    public async Task<Guid?> GetWarehouseIdForAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var warehouseIdQuery = from appointment in context.DockAppointments
                               where appointment.Id == appointmentId
                               join dock in context.Warehouses.SelectMany(w => w.Docks)
                                 on appointment.DockId equals dock.Id
                               select dock.WarehouseId;

        var warehouseId = await warehouseIdQuery.FirstOrDefaultAsync(cancellationToken);

        return warehouseId == Guid.Empty ? null : warehouseId;
    }
}