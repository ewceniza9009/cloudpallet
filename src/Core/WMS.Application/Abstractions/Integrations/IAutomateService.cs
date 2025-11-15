namespace WMS.Application.Abstractions.Integrations;

public interface IAutomationService
{
    Task<bool> MoveInventoryAsync(Guid inventoryId, string fromLocation, string toLocation, CancellationToken cancellationToken);
}