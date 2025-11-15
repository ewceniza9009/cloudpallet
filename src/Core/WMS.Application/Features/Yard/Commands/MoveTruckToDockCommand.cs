using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;
using WMS.Domain.Events;   

namespace WMS.Application.Features.Yard.Commands;

public record MoveTruckToDockCommand(Guid YardSpotId, Guid AppointmentId) : IRequest;

public class MoveTruckToDockCommandHandler(
    IWarehouseRepository warehouseRepository,
    IDockAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork,
    IPublisher publisher) : IRequestHandler<MoveTruckToDockCommand>  
{
    public async Task Handle(MoveTruckToDockCommand request, CancellationToken cancellationToken)
    {
        var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Appointment not found.");
        var yardSpot = await warehouseRepository.GetYardSpotByIdAsync(request.YardSpotId, cancellationToken)
            ?? throw new KeyNotFoundException("Yard spot not found.");
        var dock = await warehouseRepository.GetDockByIdAsync(appointment.DockId, cancellationToken)
            ?? throw new KeyNotFoundException("Dock not found.");

        yardSpot.Vacate();
        dock.Occupy(request.AppointmentId);
        appointment.UpdateStatus(AppointmentStatus.InProgress);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new DockStatusChangedEvent(dock.Id, false, dock.CurrentAppointmentId), cancellationToken);
    }
}