using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using WMS.Api.Hubs;
using WMS.Api.Hubs.Interfaces;
using WMS.Infrastructure.Persistence;

namespace WMS.Api.Services;

// Simple record to hold the update we are waiting to apply
internal record PendingTemperatureUpdate(
    Guid RoomId,
    string RoomName,
    decimal NewTemperature,
    Guid WarehouseId,
    DateTime ScheduledTime);

public class TemperatureSimulationService(
    ILogger<TemperatureSimulationService> logger,
    IHubContext<TemperatureHub, ITemperatureHubClient> tempHubContext,
    IHubContext<NotificationHub, INotificationHubClient> notificationHubContext,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly Random _random = new();

    // Stores the current state
    private readonly ConcurrentDictionary<Guid, (string Name, decimal Temperature, Guid WarehouseId)> _roomTemperatures = new();

    // Stores updates that have been notified but not yet applied (Thread-safe bag or list)
    private readonly List<PendingTemperatureUpdate> _pendingUpdates = new();

    // Lock object for the list since standard List is not thread-safe
    private readonly object _updateLock = new();

    // CONSTANT: How far in advance to notify? (Set to 1 hour as requested)
    // Note: For testing, you might want to change this to TimeSpan.FromSeconds(10)
    private readonly TimeSpan _notificationLeadTime = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Real-Time Temperature Service is starting.");

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            await InitializeRoomTemperatures(stoppingToken);

            // Track when we last scheduled a NEW event
            var nextScheduleTime = DateTime.UtcNow;

            while (!stoppingToken.IsCancellationRequested)
            {
                // 1. PHASE ONE: GENERATE AND NOTIFY (The "Pre-Warning")
                if (DateTime.UtcNow >= nextScheduleTime && !_roomTemperatures.IsEmpty)
                {
                    ScheduleNewUpdate();
                    // Schedule the next generation event (e.g., create a new pending update every 1 minute)
                    nextScheduleTime = DateTime.UtcNow.AddMinutes(1);
                }

                // 2. PHASE TWO: EXECUTE MATURE UPDATES (The "Actual Update")
                await ProcessPendingUpdates();

                // Check fairly frequently (every 1 second) so we don't miss the "hour" mark by too much
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
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

    private void ScheduleNewUpdate()
    {
        // Pick a random room
        var roomToUpdateId = _roomTemperatures.Keys.ElementAt(_random.Next(_roomTemperatures.Count));

        if (_roomTemperatures.TryGetValue(roomToUpdateId, out var currentData))
        {
            // Calculate the new temperature NOW
            var fluctuation = ((decimal)_random.NextDouble() - 0.5m) * 0.2m;
            var newTemp = Math.Round(currentData.Temperature + fluctuation, 2);

            // Determine when this should actually happen
            var executionTime = DateTime.UtcNow.Add(_notificationLeadTime);

            // Create the notification message
            var message = $"WARNING: Temperature in {currentData.Name} will change to {newTemp}°C in 1 Hour.";

            // SEND NOTIFICATION IMMEDIATELY
            // We use Fire-and-forget here so we don't block the scheduling logic
            _ = Task.Run(async () =>
            {
                var notificationDto = new NotificationDto("thermostat-warning", message, DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc));
                await notificationHubContext.Clients.All.ReceiveNotification(notificationDto);
            });

            logger.LogInformation("Scheduled update for {Room} to {Temp} at {Time}", currentData.Name, newTemp, executionTime);

            // Store the update to be applied later
            lock (_updateLock)
            {
                _pendingUpdates.Add(new PendingTemperatureUpdate(
                    roomToUpdateId,
                    currentData.Name,
                    newTemp,
                    currentData.WarehouseId,
                    executionTime
                ));
            }
        }
    }

    private async Task ProcessPendingUpdates()
    {
        List<PendingTemperatureUpdate> updatesToApply = new();

        lock (_updateLock)
        {
            var now = DateTime.UtcNow;
            // Find updates where the scheduled time has passed
            updatesToApply = _pendingUpdates.Where(u => u.ScheduledTime <= now).ToList();

            // Remove them from the pending list
            foreach (var update in updatesToApply)
            {
                _pendingUpdates.Remove(update);
            }
        }

        // Apply the updates
        foreach (var update in updatesToApply)
        {
            // Update local state
            _roomTemperatures[update.RoomId] = (update.RoomName, update.NewTemperature, update.WarehouseId);

            // Send the LIVE dashboard update
            await tempHubContext.Clients.All.ReceiveTemperatureAlert(
                update.WarehouseId,
                update.RoomId,
                update.RoomName,
                update.NewTemperature,
                -15.0m // Assuming a threshold for demo purposes
            );

            logger.LogInformation("Applied delayed update for {Room}: {Temp}°C", update.RoomName, update.NewTemperature);
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