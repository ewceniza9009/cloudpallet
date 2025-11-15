using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Dock.Appointments.Queries;

public record AppointmentDetailsDto(Guid AppointmentId, Guid SupplierId, Guid AccountId);

public record GetAppointmentDetailsQuery(Guid AppointmentId) : IRequest<AppointmentDetailsDto?>;

public class GetAppointmentDetailsQueryHandler(IDockAppointmentRepository appointmentRepository)
    : IRequestHandler<GetAppointmentDetailsQuery, AppointmentDetailsDto?>
{
    public async Task<AppointmentDetailsDto?> Handle(GetAppointmentDetailsQuery request, CancellationToken cancellationToken)
    {
        var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);

        if (appointment is null)
        {
            return null;
        }

        return new AppointmentDetailsDto(appointment.Id, appointment.SupplierId, appointment.AccountId);
    }
}