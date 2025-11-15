using WMS.Domain.Enums;

public record DockAppointmentDto
{
    public Guid Id { get; init; }
    public Guid DockId { get; init; }
    public string DockName { get; init; } = string.Empty;  
    public Guid TruckId { get; init; }
    public string LicensePlate { get; init; } = string.Empty;
    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime { get; init; }
    public AppointmentStatus Status { get; init; }
    public string? YardSpotNumber { get; init; }  
}