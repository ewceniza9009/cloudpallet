using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Dock.Appointments.Commands;

public class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand, Unit>
{
    private readonly IDockAppointmentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelAppointmentCommandHandler(IDockAppointmentRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _repository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            throw new KeyNotFoundException($"Appointment with ID {request.AppointmentId} not found.");
        }

        appointment.Cancel();

        await _repository.UpdateAsync(appointment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
