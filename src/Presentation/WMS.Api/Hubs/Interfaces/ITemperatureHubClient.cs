namespace WMS.Api.Hubs.Interfaces;

public interface ITemperatureHubClient
{
    Task ReceiveTemperatureAlert(Guid warehouseId, Guid roomId, string roomName, decimal currentTemperature, decimal threshold);
    Task UpdateEnergyDashboard(object energyData);
}