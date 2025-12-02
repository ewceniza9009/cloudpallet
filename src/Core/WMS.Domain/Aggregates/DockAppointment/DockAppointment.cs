using System.Text.Json.Serialization;
using WMS.Domain.Aggregates.Warehouse;
using WMS.Domain.Enums;
using WMS.Domain.Events;
using WMS.Domain.Shared;
using WMS.Domain.Entities;

namespace WMS.Domain.Aggregates.DockAppointment;

public class DockAppointment : AggregateRoot<Guid>
{
    [JsonInclude]
    public Guid DockId { get; private set; }
    [JsonInclude]
    public Guid? TruckId { get; private set; }
    [JsonInclude]
    public Guid SupplierId { get; private set; }  
    [JsonInclude]
    public Guid AccountId { get; private set; }  
    [JsonInclude]
    public DateTime StartDateTime { get; private set; }
    [JsonInclude]
    public DateTime EndDateTime { get; private set; }
    [JsonInclude]
    public AppointmentType Type { get; private set; }
    [JsonInclude]
    public AppointmentStatus Status { get; private set; }
    [JsonInclude]
    public Guid? CargoManifestId { get; private set; }

    public Truck? Truck { get; private set; }
    public Dock Dock { get; private set; } = null!;

    [JsonConstructor]
    private DockAppointment() : base(Guid.Empty) { }

    private DockAppointment(Guid id, Guid dockId, Guid? truckId, Guid supplierId, Guid accountId, DateTime start, DateTime end, AppointmentType type) : base(id)
    {
        if (start >= end) throw new ArgumentException("Start time must be before end time.");
        DockId = dockId;
        TruckId = truckId;
        SupplierId = supplierId;  
        AccountId = accountId;  
        StartDateTime = start;
        EndDateTime = end;
        Type = type;
        Status = AppointmentStatus.Scheduled;
    }

    public static DockAppointment Create(Guid dockId, Guid? truckId, Guid supplierId, Guid accountId, DateTime start, DateTime end, AppointmentType type)
    {
        var appointment = new DockAppointment(Guid.NewGuid(), dockId, truckId, supplierId, accountId, start, end, type);
        appointment.AddDomainEvent(new DockAppointmentScheduledEvent(appointment.Id, appointment.DockId, appointment.StartDateTime, appointment.EndDateTime));
        return appointment;
    }
    public void UpdateStatus(AppointmentStatus newStatus)
    {
        Status = newStatus;
    }

    public void Cancel()
    {
        if (Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot cancel an appointment that is already completed or cancelled.");
        }
        Status = AppointmentStatus.Cancelled;
    }

    public void Reschedule(DateTime newStart, DateTime newEnd)
    {
        if (newStart >= newEnd) throw new ArgumentException("Start time must be before end time.");
        if (Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot reschedule an appointment that is already completed or cancelled.");
        }
        StartDateTime = newStart;
        EndDateTime = newEnd;
    }
}
