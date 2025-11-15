using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;  
using WMS.Domain.Events;

namespace WMS.Application.Features.Yard.Commands;

public record TruckCheckInCommand(Guid TruckId, Guid YardSpotId) : IRequest<Guid>;

public class TruckCheckInCommandHandler(
    IWarehouseRepository warehouseRepository,
    IDockAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork,
    IPublisher publisher) : IRequestHandler<TruckCheckInCommand, Guid>
{
    public async Task<Guid> Handle(TruckCheckInCommand request, CancellationToken cancellationToken)
    {
        var yardSpot = await warehouseRepository.GetYardSpotByIdAsync(request.YardSpotId, cancellationToken)
            ?? throw new KeyNotFoundException("Selected yard spot not found.");

        if (yardSpot.Status != YardSpotStatus.Available)
        {
            throw new InvalidOperationException($"Yard spot {yardSpot.SpotNumber} is already occupied.");
        }

        var today = DateTime.Now;
        var tomorrow = today.AddDays(1);

        var appointment = (await appointmentRepository.GetAppointmentsForTruckByDateAsync(
            request.TruckId, today, tomorrow, cancellationToken))
            .FirstOrDefault(a => a.Status == AppointmentStatus.Scheduled);

        if (appointment == null)
        {
            throw new InvalidOperationException("No scheduled appointment found for this truck today.");
        }

        yardSpot.Occupy(request.TruckId);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new DockCheckInEvent(appointment.Id, request.TruckId, yardSpot.Id), cancellationToken);

        return yardSpot.Id;
    }
}