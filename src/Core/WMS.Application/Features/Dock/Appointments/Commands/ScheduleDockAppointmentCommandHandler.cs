using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Aggregates.DockAppointment;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Dock.Appointments.Commands;

public class ScheduleDockAppointmentCommandHandler(
    IDockAppointmentRepository dockAppointmentRepository,
    ITruckRepository truckRepository,
    ICarrierRepository carrierRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ScheduleDockAppointmentCommand, Guid>
{
    public async Task<Guid> Handle(ScheduleDockAppointmentCommand request, CancellationToken cancellationToken)
    {
        var isSlotAvailable = await dockAppointmentRepository.DoesAppointmentOverlapAsync(
            request.DockId,
            request.StartDateTime,
            request.EndDateTime,
            cancellationToken);

        if (!isSlotAvailable)
        {
            throw new InvalidOperationException("The requested time slot is unavailable.");
        }

        var truck = await truckRepository.GetByLicensePlateAsync(request.LicensePlate, cancellationToken);
        if (truck is null)
        {
            var defaultCarrier = await carrierRepository.GetOrCreateDefaultCarrierAsync(cancellationToken);
            truck = Truck.Create(defaultCarrier.Id, request.LicensePlate, "Default", 10000, 30);
            await truckRepository.AddAsync(truck, cancellationToken);
        }

        var appointment = DockAppointment.Create(
            request.DockId,
            truck.Id,
            request.SupplierId,
            request.AccountId,
            request.StartDateTime,
            request.EndDateTime,
            request.Type);        

        await dockAppointmentRepository.AddAsync(appointment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return appointment.Id;
    }
}