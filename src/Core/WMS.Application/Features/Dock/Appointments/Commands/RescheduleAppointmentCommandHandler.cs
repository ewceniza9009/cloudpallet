using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Dock.Appointments.Commands;

public class RescheduleAppointmentCommandHandler : IRequestHandler<RescheduleAppointmentCommand, Unit>
{
    private readonly IDockAppointmentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RescheduleAppointmentCommandHandler(IDockAppointmentRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _repository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            throw new KeyNotFoundException($"Appointment with ID {request.AppointmentId} not found.");
        }

        // Basic validation: Ensure end time is after start time
        if (request.NewEndTime <= request.NewStartTime)
        {
            throw new ArgumentException("End time must be after start time.");
        }

        // TODO: Add overlap checking logic here if needed, similar to ScheduleDockAppointmentCommandHandler

        appointment.Reschedule(request.NewStartTime, request.NewEndTime);
        
        await _repository.UpdateAsync(appointment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
