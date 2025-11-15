using MediatR;
using WMS.Domain.Enums;   

namespace WMS.Application.Features.Dock.Appointments.Commands;

public record ScheduleDockAppointmentCommand(
    Guid DockId,
    string LicensePlate,
    Guid SupplierId,
    Guid AccountId,
    DateTime StartDateTime,
    DateTime EndDateTime,
    AppointmentType Type) : IRequest<Guid>;    