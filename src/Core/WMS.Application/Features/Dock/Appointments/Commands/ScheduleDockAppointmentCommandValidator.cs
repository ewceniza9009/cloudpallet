namespace WMS.Application.Features.Dock.Appointments.Commands;
using FluentValidation;

public class ScheduleDockAppointmentCommandValidator : AbstractValidator<ScheduleDockAppointmentCommand>
{
    public ScheduleDockAppointmentCommandValidator()
    {
        RuleFor(x => x.DockId).NotEmpty();
        RuleFor(x => x.LicensePlate).NotEmpty().WithMessage("License plate is required.");

        RuleFor(x => x.StartDateTime).NotEmpty().GreaterThan(DateTime.UtcNow)
            .WithMessage("Appointment must be in the future.");

        RuleFor(x => x.EndDateTime)
            .NotEmpty()
            .GreaterThan(x => x.StartDateTime)
            .WithMessage("End time must be after start time.");
    }
}