using MediatR;

namespace WMS.Application.Features.Dock.Appointments.Commands;

public record CancelAppointmentCommand(Guid AppointmentId) : IRequest<Unit>;
