using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Aggregates.Warehouse;
using WMS.Application.Features.LocationSetup.Queries;
using System.Linq.Dynamic.Core;
using AppPagedResult = WMS.Application.Common.Models.PagedResult<WMS.Domain.Aggregates.Warehouse.Location>;

namespace WMS.Infrastructure.Persistence.Repositories;

public class WarehouseAdminRepository : IWarehouseAdminRepository
{
    private readonly WmsDbContext _context;

    public WarehouseAdminRepository(WmsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken)
    {
        await _context.Warehouses.AddAsync(warehouse, cancellationToken);
    }

    public async Task<IEnumerable<Warehouse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Warehouses
            .AsNoTracking()
            .Include(w => w.Address)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken, bool? withTracking = false)
    {
        if (withTracking ?? false) 
        {
            return await _context.Warehouses
            .Include(w => w.Address)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        }

        return await _context.Warehouses
            .AsNoTracking()
            .Include(w => w.Address)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public void Remove(Warehouse warehouse)
    {
        _context.Warehouses.Remove(warehouse);
    }

    public async Task<IEnumerable<Room>> GetAllRoomsAsync(CancellationToken cancellationToken)
    {
        return await _context.Rooms
            .AsNoTracking()
            .Include(r => r.Locations)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Room?> GetRoomByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Rooms
            .Include(r => r.Locations)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> DoesLocationExist(Guid roomId, string bay, int row, int col, int level, CancellationToken cancellationToken)
    {
        var barcode = $"{bay}-{row}-{col}-{level}";
        return await _context.Locations.AnyAsync(l =>
            l.RoomId == roomId &&
            l.Barcode == barcode,
            cancellationToken);
    }

    public async Task<AppPagedResult> GetLocationsForRoomAsync(GetLocationsForRoomQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Locations
            .AsNoTracking()
            .Where(l => l.RoomId == request.RoomId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(l => l.Barcode.ToLower().Contains(term) || l.Bay.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            var sortExpression = $"{request.SortBy} {request.SortDirection ?? "asc"}";
            query = query.OrderBy(sortExpression);
        }
        else
        {
            query = query.OrderBy(l => l.Bay).ThenBy(l => l.Row).ThenBy(l => l.Column).ThenBy(l => l.Level);
        }

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new AppPagedResult { Items = items, TotalCount = totalCount };
    }

    public async Task<Location?> GetLocationByIdAsync(Guid locationId, CancellationToken cancellationToken)
    {
        return await _context.Locations.FindAsync(new object[] { locationId }, cancellationToken);
    }

    public void RemoveLocation(Location location)
    {
        _context.Locations.Remove(location);
    }

    public void AddRoom(Room room)
    {
        _context.Rooms.Add(room);
    }

    public async Task<Warehouse?> GetByIdWithDocksAndYardSpotsAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        return await _context.Warehouses
            .AsNoTracking()
            .Include(w => w.Docks)
            .Include(w => w.YardSpots)
            .FirstOrDefaultAsync(w => w.Id == warehouseId, cancellationToken);
    }

    public async Task<Dock?> GetDockByIdAsync(Guid dockId, CancellationToken cancellationToken)
    {
        return await _context.Docks.FindAsync(new object[] { dockId }, cancellationToken);
    }

    public void RemoveDock(Dock dock)
    {
        _context.Docks.Remove(dock);
    }

    public async Task<YardSpot?> GetYardSpotByIdAsync(Guid yardSpotId, CancellationToken cancellationToken)
    {
        return await _context.YardSpots.FindAsync(new object[] { yardSpotId }, cancellationToken);
    }

    public void RemoveYardSpot(YardSpot yardSpot)
    {
        _context.YardSpots.Remove(yardSpot);
    }
}