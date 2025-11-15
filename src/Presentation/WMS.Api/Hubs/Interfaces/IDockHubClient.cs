namespace WMS.Api.Hubs.Interfaces;

public record DockStatusUpdate(Guid DockId, bool IsAvailable, Guid? AppointmentId);

public interface IDockHubClient
{
    Task ReceiveDockStatusUpdate(DockStatusUpdate update);
}