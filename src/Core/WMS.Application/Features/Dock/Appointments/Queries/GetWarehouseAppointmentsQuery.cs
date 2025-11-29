using MediatR;

namespace WMS.Application.Features.Dock.Appointments.Queries;

public record GetWarehouseAppointmentsQuery(Guid WarehouseId, DateTime StartDate, DateTime EndDate) : IRequest<IEnumerable<DockAppointmentDto>>;
