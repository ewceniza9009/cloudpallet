using MediatR;
using WMS.Application.Abstractions.Persistence;
using AutoMapper;

namespace WMS.Application.Features.Yard.Queries;

public record YardAppointmentDto
{
    public Guid AppointmentId { get; init; }
    public Guid TruckId { get; init; }
    public string CarrierName { get; init; } = string.Empty;
    public string LicensePlate { get; init; } = string.Empty;
    public DateTime AppointmentTime { get; init; }       
    public string Status { get; init; } = string.Empty;
    public string DockName { get; init; } = string.Empty;
}

public record GetTodaysAppointmentsQuery(Guid WarehouseId, DateTime StartDate, DateTime EndDate) : IRequest<IEnumerable<YardAppointmentDto>>;

public class GetTodaysAppointmentsQueryHandler(IReadOnlyAppointmentRepository appointmentRepository, IMapper mapper)
    : IRequestHandler<GetTodaysAppointmentsQuery, IEnumerable<YardAppointmentDto>>
{
    public async Task<IEnumerable<YardAppointmentDto>> Handle(GetTodaysAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var appointments = await appointmentRepository.GetAppointmentsForDateRangeAsync(request.WarehouseId, request.StartDate, request.EndDate, cancellationToken);

        return mapper.Map<IEnumerable<YardAppointmentDto>>(appointments);
    }
}