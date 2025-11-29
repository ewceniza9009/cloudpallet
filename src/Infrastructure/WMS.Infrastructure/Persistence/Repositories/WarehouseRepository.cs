// ---- File: src/Infrastructure/WMS.Infrastructure/Persistence/Repositories/WarehouseRepository.cs ----
using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.Inventory.Queries;
using WMS.Application.Features.Warehouse.Queries;
using WMS.Application.Features.Yard.Queries;
using WMS.Domain.Aggregates.Warehouse;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Persistence.Repositories;

public class WarehouseRepository(WmsDbContext context) : IWarehouseRepository
{
    // --- START: THE FINAL, GUARANTEED CORRECT METHOD ---
    public async Task<LocationOverviewDto?> GetLocationOverviewAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        // STEP 1: Fetch all rooms for the warehouse. This is a simple, reliable query.
        var rooms = await context.Rooms
            .Where(r => r.WarehouseId == warehouseId)
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        if (!rooms.Any())
        {
            return null;
        }

        var roomIds = rooms.Select(r => r.Id).ToList();

        // STEP 2: Fetch all locations for those rooms in a separate, reliable query.
        var allLocations = await context.Locations
            .Where(l => roomIds.Contains(l.RoomId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var locationIds = allLocations.Select(l => l.Id).ToList();

        // STEP 3: Fetch all inventory weights in a single, efficient database trip.
        var inventoryWeights = await context.MaterialInventories
            .AsNoTracking()
            .Where(i => locationIds.Contains(i.LocationId))
            .GroupBy(i => i.LocationId)
            .Select(g => new { LocationId = g.Key, CurrentWeight = g.Sum(i => i.WeightActual.Value) })
            .ToDictionaryAsync(x => x.LocationId, x => x.CurrentWeight, cancellationToken);

        // STEP 4: Build the final DTO structure entirely in memory from the clean, correct data.
        // This avoids any complex LINQ translation bugs.
        var roomDtos = new List<RoomDto>();
        foreach (var room in rooms)
        {
            var locationsInRoom = allLocations.Where(l => l.RoomId == room.Id).ToList();

            var bayDtos = locationsInRoom
                .GroupBy(l => l.Bay)
                .OrderBy(bg => bg.Key)
                .Select(bayGroup =>
                {
                    var locationDtos = bayGroup.Select(loc =>
                    {
                        inventoryWeights.TryGetValue(loc.Id, out var currentWeight);
                        var capacity = loc.CapacityWeight.Value;
                        var util = capacity > 0 ? Math.Round((double)currentWeight / (double)capacity * 100, 2) : 0;
                        return new LocationDto(loc.Row, loc.Column, loc.Level, currentWeight, capacity, util, GetStatus(util));
                    }).ToList();

                    var bayCurrentWeight = locationDtos.Sum(l => l.CurrentWeight);
                    var bayCapacityWeight = locationDtos.Sum(l => l.CapacityWeight);
                    var bayUtil = bayCapacityWeight > 0 ? Math.Round((double)bayCurrentWeight / (double)bayCapacityWeight * 100, 2) : 0;

                    return new BayDto(bayGroup.Key, bayCurrentWeight, bayCapacityWeight, bayUtil, GetStatus(bayUtil), locationDtos);
                }).ToList();

            var roomCurrentWeight = bayDtos.Sum(b => b.CurrentWeight);
            var roomCapacityWeight = bayDtos.Sum(b => b.CapacityWeight);
            var roomUtil = roomCapacityWeight > 0 ? Math.Round((double)roomCurrentWeight / (double)roomCapacityWeight * 100, 2) : 0;

            roomDtos.Add(new RoomDto(room.Name, roomCurrentWeight, roomCapacityWeight, roomUtil, bayDtos));
        }

        return new LocationOverviewDto(roomDtos);
    }
    // --- END: THE FINAL, GUARANTEED CORRECT METHOD ---

    private static string GetStatus(double utilization) => utilization switch
    {
        > 100 => "Over",
        > 90 => "Full",
        > 60 => "Approaching",
        > 0 => "Partial",
        _ => "Empty"
    };

    // ... The rest of the methods in this file are unchanged and correct ...
    public async Task<Warehouse?> GetByIdWithYardSpotsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Warehouses
            .Include(w => w.YardSpots)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<YardSpot?> GetYardSpotByIdAsync(Guid yardSpotId, CancellationToken cancellationToken)
    {
        return await context.Warehouses
            .SelectMany(w => w.YardSpots)
            .FirstOrDefaultAsync(ys => ys.Id == yardSpotId, cancellationToken);
    }

    public async Task<Dock?> GetDockByIdAsync(Guid dockId, CancellationToken cancellationToken)
    {
        return await context.Docks.FirstOrDefaultAsync(d => d.Id == dockId, cancellationToken);
    }

    public async Task<IEnumerable<Pallet>> GetPalletsInStagingAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        var stagingLocationIds = await context.Warehouses
            .Where(w => w.Id == warehouseId)
            .SelectMany(w => w.Rooms)
            .SelectMany(r => r.Locations)
            .Where(l => l.ZoneType == LocationType.Staging)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        if (!stagingLocationIds.Any())
        {
            return Enumerable.Empty<Pallet>();
        }

        var palletsInStaging = await context.Pallets
            .AsNoTracking()
            .Include(p => p.Lines)
            .Where(p => context.MaterialInventories.Any(mi =>
                mi.PalletId == p.Id &&
                stagingLocationIds.Contains(mi.LocationId)))
            .Where(p => p.Receiving.Status == ReceivingStatus.Completed)
            .ToListAsync(cancellationToken);

        var completedPutawayPalletIds = await context.PutawayTransactions
            .Where(pt => pt.Status == TransactionStatus.Completed)
            .Select(pt => pt.PalletId)
            .ToListAsync(cancellationToken);

        var candidatePallets = palletsInStaging.Where(p => !completedPutawayPalletIds.Contains(p.Id));

        var readyForPutaway = candidatePallets.Where(p =>
            p.Lines.Any() &&
            p.Lines.All(l => l.Status == PalletLineStatus.Processed)
        );

        return readyForPutaway;
    }

    public async Task<IEnumerable<Pallet>> GetStoredPalletsAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        var storageLocationIds = await context.Warehouses
            .Where(w => w.Id == warehouseId)
            .SelectMany(w => w.Rooms)
            .SelectMany(r => r.Locations)
            .Where(l => l.ZoneType == LocationType.Storage)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        if (!storageLocationIds.Any())
        {
            return Enumerable.Empty<Pallet>();
        }

        var palletIds = await context.MaterialInventories
            .Where(mi => storageLocationIds.Contains(mi.LocationId))
            .Select(mi => mi.PalletId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await context.Pallets
            .AsNoTracking()
            .Include(p => p.Lines)
            .Include(p => p.Inventory)
                .ThenInclude(mi => mi.Location)
            .Where(p => palletIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RoomWithPalletsDto>> GetStoredPalletsByRoomAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        // Get all rooms for the warehouse first
        var rooms = await context.Rooms
            .AsNoTracking()
            .Where(r => r.WarehouseId == warehouseId)
            .OrderBy(r => r.Name)
            .Select(r => new { r.Id, r.Name })
            .ToListAsync(cancellationToken);

        var result = new List<RoomWithPalletsDto>();

        // Loop through each room to find pallets
        foreach (var room in rooms)
        {
            var palletsInRoom = await context.Pallets
                .AsNoTracking()
                // Eagerly load the data we need
                .Include(p => p.Inventory)
                    .ThenInclude(i => i.Material) // Load Inventory -> Material
                .Include(p => p.Inventory)
                    .ThenInclude(i => i.Location) // Load Inventory -> Location
                                                  // Find pallets that have any inventory in this room
                .Where(p => p.Inventory.Any(i => i.Location.RoomId == room.Id && i.Location.ZoneType == LocationType.Storage && i.Quantity > 0))
                .OrderBy(p => p.Barcode)
                .Select(p => new StoredPalletDetailDto
                {
                    PalletId = p.Id,
                    PalletBarcode = p.Barcode,
                    // Get location from the first inventory item. Assumes all items on a pallet are in one location.
                    CurrentLocationId = p.Inventory.First().LocationId,
                    CurrentLocationBarcode = p.Inventory.First().Location.Barcode,

                    // Project from the p.Inventory collection (current stock)
                    // NOT from p.Lines (original receiving data)
                    Lines = p.Inventory
                        .Where(i => i.Quantity > 0) // Only show items with stock
                        .Select(i => new PalletLineItemDto
                        {
                            InventoryId = i.Id,
                            MaterialName = i.Material.Name,
                            Quantity = i.Quantity,
                            Barcode = i.Barcode // This is the LPN
                        }).ToList()
                })
                .ToListAsync(cancellationToken);

            if (palletsInRoom.Any())
            {
                result.Add(new RoomWithPalletsDto
                {
                    RoomName = room.Name,
                    Pallets = palletsInRoom
                });
            }
        }

        return result;
    }

    public async Task<IEnumerable<OccupiedYardSpotDto>> GetOccupiedYardSpotsAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        var query = from ys in context.YardSpots
                    where ys.WarehouseId == warehouseId && ys.Status == Domain.Enums.YardSpotStatus.Occupied && ys.CurrentTruckId != null
                    join t in context.Trucks on ys.CurrentTruckId equals t.Id
                    let appointment = context.DockAppointments
                        .Where(da => da.TruckId == t.Id)
                        .OrderByDescending(da => da.StartDateTime)
                        .FirstOrDefault()
                    orderby ys.SpotNumber
                    select new
                    {
                        ys.Id,
                        ys.SpotNumber,
                        t.LicensePlate,
                        TruckId = t.Id,
                        AppointmentId = appointment != null ? appointment.Id : Guid.Empty,
                        OccupiedSince = DateTime.SpecifyKind(ys.OccupiedSince!.Value, DateTimeKind.Utc)
                    };

        var result = await query.ToListAsync(cancellationToken);

        return result.Select(r => new OccupiedYardSpotDto(
            r.Id,
            r.SpotNumber,
            r.LicensePlate,
            r.TruckId,
            r.AppointmentId,
            r.OccupiedSince
        ));
    }

    public Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken)
    {
        context.Warehouses.Add(warehouse);
        return Task.CompletedTask;
    }

    public async Task<Room?> GetRoomByLocationIdAsync(Guid locationId, CancellationToken cancellationToken)
    {
        // This query needs to use the DbContext directly now.
        return await context.Rooms
            .Include(r => r.Locations)
            .FirstOrDefaultAsync(r => r.Locations.Any(l => l.Id == locationId), cancellationToken);
    }

    public async Task<IEnumerable<StoredPalletSearchResultDto>> SearchStoredPalletsAsync(
        Guid warehouseId,
        Guid? accountId,
        Guid? materialId,
        string? barcodeQuery,
        CancellationToken cancellationToken)
    {
        // 1. Filter MaterialInventories to find matching Pallet IDs
        var query = context.MaterialInventories.AsNoTracking()
            .Where(mi => mi.Location.Room.WarehouseId == warehouseId &&
                         mi.Location.ZoneType == LocationType.Storage &&
                         mi.Quantity > 0);

        if (accountId.HasValue)
        {
            query = query.Where(mi => mi.AccountId == accountId.Value);
        }
        if (materialId.HasValue)
        {
            query = query.Where(mi => mi.MaterialId == materialId.Value);
        }
        if (!string.IsNullOrWhiteSpace(barcodeQuery))
        {
            // Use Contains to support new barcode format like (00-1234567)...
            query = query.Where(mi => mi.Pallet.Barcode.Contains(barcodeQuery) ||
                                     mi.Barcode.Contains(barcodeQuery));
        }

        // Get distinct Pallet IDs first (Limit to 50)
        var palletIds = await query
            .Select(mi => mi.PalletId)
            .Distinct()
            .Take(50)
            .ToListAsync(cancellationToken);

        if (!palletIds.Any())
        {
            return Enumerable.Empty<StoredPalletSearchResultDto>();
        }

        // 2. Fetch details for the found pallets
        var pallets = await context.Pallets.AsNoTracking()
            .Where(p => palletIds.Contains(p.Id))
            .Include(p => p.Account)
            .Include(p => p.Inventory)
                .ThenInclude(i => i.Material)
            .Include(p => p.Inventory)
                .ThenInclude(i => i.Location)
            .ToListAsync(cancellationToken);

        // 3. Map to DTO in memory
        return pallets.Select(p =>
        {
            var activeInventory = p.Inventory.Where(i => i.Quantity > 0).ToList();
            var firstInv = activeInventory.FirstOrDefault();
            var locationName = firstInv?.Location.Barcode ?? "Unknown";
            var materials = activeInventory.Select(i => i.Material.Name).Distinct().ToList();

            return new StoredPalletSearchResultDto
            {
                PalletId = p.Id,
                PalletBarcode = p.Barcode,
                LocationName = locationName,
                AccountName = p.Account.Name,
                MaterialSummary = materials.Count > 1
                    ? $"{materials.First()} + {materials.Count - 1} other(s)"
                    : (materials.FirstOrDefault() ?? "N/A"),
                Quantity = activeInventory.Sum(i => i.Quantity)
            };
        });
    }
}