using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Dock.Appointments.Commands;

// The command now returns the GUID of the receiving session
public record StartUnloadingCommand(Guid DockId) : IRequest<Guid>;

public class StartUnloadingCommandHandler(
    IWarehouseRepository warehouseRepository,
    IDockAppointmentRepository appointmentRepository,
    IReceivingTransactionRepository receivingRepository, // Add receiving repo
    IUnitOfWork unitOfWork) : IRequestHandler<StartUnloadingCommand, Guid>
{
    public async Task<Guid> Handle(StartUnloadingCommand request, CancellationToken cancellationToken)
    {
        var dock = await warehouseRepository.GetDockByIdAsync(request.DockId, cancellationToken)
            ?? throw new KeyNotFoundException("Dock not found.");

        if (!dock.CurrentAppointmentId.HasValue)
        {
            throw new InvalidOperationException("Dock is not currently occupied by an appointment.");
        }

        var appointment = await appointmentRepository.GetByIdAsync(dock.CurrentAppointmentId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("Associated appointment not found.");

        appointment.UpdateStatus(AppointmentStatus.Unloading);

        // --- NEW LOGIC START ---

        // Check if a receiving session already exists for this appointment
        var receivingSession = await receivingRepository.GetByAppointmentIdAsync(appointment.Id, cancellationToken);

        if (receivingSession is null)
        {
            // If it doesn't exist, create a new one
            receivingSession = Receiving.Create(appointment.SupplierId, appointment.Id, appointment.AccountId);
            await receivingRepository.AddAsync(receivingSession, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return receivingSession.Id; // Return the ID of the session
        // --- NEW LOGIC END ---
    }
}