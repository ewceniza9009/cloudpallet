using AutoMapper;
using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Dock.Appointments.Queries;

public record GetAppointmentsByPlateQuery(string LicensePlate) : IRequest<IEnumerable<DockAppointmentDto>>;

public class GetAppointmentsByPlateQueryHandler(IReadOnlyAppointmentRepository repository, IMapper mapper)
    : IRequestHandler<GetAppointmentsByPlateQuery, IEnumerable<DockAppointmentDto>>
{
    public async Task<IEnumerable<DockAppointmentDto>> Handle(GetAppointmentsByPlateQuery request, CancellationToken cancellationToken)
    {
        var appointments = await repository.GetAppointmentsByLicensePlateAsync(request.LicensePlate, cancellationToken);
        return mapper.Map<IEnumerable<DockAppointmentDto>>(appointments);
    }
}