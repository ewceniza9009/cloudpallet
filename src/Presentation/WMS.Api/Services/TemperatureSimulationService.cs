using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using WMS.Api.Hubs;
using WMS.Api.Hubs.Interfaces;
using WMS.Infrastructure.Persistence;

namespace WMS.Api.Services;

public class TemperatureSimulationService(
    ILogger<TemperatureSimulationService> logger,
    IHubContext<TemperatureHub, ITemperatureHubClient> tempHubContext,
    IHubContext<NotificationHub, INotificationHubClient> notificationHubContext,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly Random _random = new();
    private readonly ConcurrentDictionary<Guid, (string Name, decimal Temperature, Guid WarehouseId)> _roomTemperatures = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Real-Time Temperature Service is starting.");

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            await InitializeRoomTemperatures(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

                if (_roomTemperatures.IsEmpty) continue;

                var roomToUpdateId = _roomTemperatures.Keys.ElementAt(_random.Next(_roomTemperatures.Count));

                if (_roomTemperatures.TryGetValue(roomToUpdateId, out var roomData))
                {
                    var fluctuation = ((decimal)_random.NextDouble() - 0.5m) * 0.2m;
                    var newTemp = Math.Round(roomData.Temperature + fluctuation, 2);

                    _roomTemperatures[roomToUpdateId] = (roomData.Name, newTemp, roomData.WarehouseId);

                    await tempHubContext.Clients.All.ReceiveTemperatureAlert(
                        roomData.WarehouseId,
                        roomToUpdateId,
                        roomData.Name,
                        newTemp,
                        -15.0m);

                    var message = $"Temp alert in {roomData.Name}: {newTemp}°C";
                    var notificationDto = new NotificationDto("thermostat", message, DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc));
                    await notificationHubContext.Clients.All.ReceiveNotification(notificationDto);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Real-Time Temperature Service is stopping gracefully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled error occurred in the Temperature Service.");
        }
        finally
        {
            logger.LogInformation("Real-Time Temperature Service has shut down.");
        }
    }

    private async Task InitializeRoomTemperatures(CancellationToken stoppingToken)
    {
        logger.LogInformation("Initializing room temperatures from database.");
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WmsDbContext>();

        var rooms = await dbContext.Warehouses
            .AsNoTracking()
            .SelectMany(w => w.Rooms.Select(r => new
            {
                RoomId = r.Id,
                RoomName = r.Name,
                r.TemperatureRange,
                WarehouseId = w.Id
            }))
            .ToListAsync(stoppingToken);

        foreach (var room in rooms)
        {
            var targetTemp = (room.TemperatureRange.MinTemperature + room.TemperatureRange.MaxTemperature) / 2;
            _roomTemperatures.TryAdd(room.RoomId, (room.RoomName, targetTemp, room.WarehouseId));
        }
        logger.LogInformation("Initialized {Count} rooms for real-time monitoring.", _roomTemperatures.Count);
    }
}