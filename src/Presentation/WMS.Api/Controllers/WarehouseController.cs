using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Features.Warehouse.Queries;   
using WMS.Infrastructure.Persistence;

namespace WMS.Api.Controllers;

public record RoomDto(Guid RoomId, string RoomName, decimal TargetTemperature);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WarehouseController : ApiControllerBase  
{
    private readonly WmsDbContext _context;

    public WarehouseController(WmsDbContext context)
    {
        _context = context;
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(LocationOverviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocationOverview([FromQuery] Guid warehouseId)
    {
        var result = await Mediator.Send(new GetLocationOverviewQuery(warehouseId));
        return Ok(result);
    }

    [HttpGet("rooms")]
    [ProducesResponseType(typeof(IEnumerable<RoomDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRooms([FromQuery] Guid warehouseId) // <-- ADDED warehouseId
    {
        if (warehouseId == Guid.Empty)
        {
            return BadRequest("A warehouseId must be provided.");
        }

        var rooms = await _context.Warehouses
            .AsNoTracking()
            .Where(w => w.Id == warehouseId) // <-- ADDED FILTER
            .SelectMany(w => w.Rooms)
            .Select(r => new RoomDto(
                r.Id,
                r.Name,
                (r.TemperatureRange.MinTemperature + r.TemperatureRange.MaxTemperature) / 2
            ))
            .ToListAsync();

        return Ok(rooms);
    }
}