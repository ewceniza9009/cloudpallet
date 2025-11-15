using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Dock.Appointments.Queries;

public class GetDockAppointmentsQueryHandler(IReadOnlyAppointmentRepository readOnlyRepository)
    : IRequestHandler<GetDockAppointmentsQuery, IEnumerable<DockAppointmentDto>>
{
    public async Task<IEnumerable<DockAppointmentDto>> Handle(GetDockAppointmentsQuery request, CancellationToken cancellationToken)
    {
        return await readOnlyRepository.GetAppointmentsWithTrucksForDockInRangeAsync(
            request.DockId,
            request.StartDate,
            request.EndDate,
            cancellationToken);
    }
}