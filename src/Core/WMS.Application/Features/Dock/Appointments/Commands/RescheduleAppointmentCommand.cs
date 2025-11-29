using MediatR;

namespace WMS.Application.Features.Dock.Appointments.Commands;

public record RescheduleAppointmentCommand(Guid AppointmentId, DateTime NewStartTime, DateTime NewEndTime) : IRequest<Unit>;
