using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;
using WMS.Domain.Events;   

namespace WMS.Application.Features.Dock.Appointments.Commands;

public record VacateDockCommand(Guid DockId) : IRequest;

public class VacateDockCommandHandler(
    IWarehouseRepository warehouseRepository,
    IDockAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork,
    IPublisher publisher) : IRequestHandler<VacateDockCommand>  
{
    public async Task Handle(VacateDockCommand request, CancellationToken cancellationToken)
    {
        var dock = await warehouseRepository.GetDockByIdAsync(request.DockId, cancellationToken)
            ?? throw new KeyNotFoundException("Dock not found.");

        if (dock.CurrentAppointmentId.HasValue)
        {
            var appointment = await appointmentRepository.GetByIdAsync(dock.CurrentAppointmentId.Value, cancellationToken);
            appointment?.UpdateStatus(AppointmentStatus.Completed);
        }

        dock.Vacate();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new DockStatusChangedEvent(dock.Id, true, null), cancellationToken);
    }
}