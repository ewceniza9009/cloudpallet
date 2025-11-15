namespace WMS.Application.Features.Dock.Appointments.Queries;
using MediatR;

public record GetDockAppointmentsQuery(Guid DockId, DateTime StartDate, DateTime EndDate) : IRequest<IEnumerable<DockAppointmentDto>>;