using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Dock.Appointments.Queries;

public class GetWarehouseAppointmentsQueryHandler(IReadOnlyAppointmentRepository readOnlyRepository)
    : IRequestHandler<GetWarehouseAppointmentsQuery, IEnumerable<DockAppointmentDto>>
{
    public async Task<IEnumerable<DockAppointmentDto>> Handle(GetWarehouseAppointmentsQuery request, CancellationToken cancellationToken)
    {
        // We need to map the domain entities to DTOs manually since the repository returns entities for this method
        // Or we can add a new method to the repository that returns DTOs directly for better performance
        // For now, let's use the existing repository method and map here, or update the repository.
        // The implementation plan said "Create handler to call readOnlyRepository.GetAppointmentsForDateRangeAsync".
        // Let's check the repository method signature again.
        // Task<IEnumerable<DockAppointment>> GetAppointmentsForDateRangeAsync(Guid warehouseId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
        
        var appointments = await readOnlyRepository.GetAppointmentsForDateRangeAsync(
            request.WarehouseId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        // Map to DTO
        return appointments.Select(da => new DockAppointmentDto
        {
            Id = da.Id,
            DockId = da.DockId,
            DockName = da.Dock?.Name ?? "Unknown Dock",
            TruckId = da.TruckId ?? Guid.Empty,
            LicensePlate = da.Truck?.LicensePlate ?? "N/A",
            StartDateTime = DateTime.SpecifyKind(da.StartDateTime, DateTimeKind.Utc),
            EndDateTime = DateTime.SpecifyKind(da.EndDateTime, DateTimeKind.Utc),
            Status = da.Status,
            YardSpotNumber = null // We might need to fetch this if needed, but for calendar view it might be optional or fetched separately
        });
    }
}
