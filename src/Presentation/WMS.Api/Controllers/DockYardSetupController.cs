using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Features.WarehouseSetup.Commands;
using WMS.Application.Features.WarehouseSetup.Queries;

namespace WMS.Api.Controllers;

[ApiController]
[Route("api/setup")]     
[Authorize(Policy = "AdminPolicy")]
public class DockYardSetupController : ApiControllerBase
{
    [HttpGet("dock-yard")]
    [ProducesResponseType(typeof(DockYardSetupDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDockYardSetup([FromQuery] Guid warehouseId)
    {
        var result = await Mediator.Send(new GetDockYardSetupQuery(warehouseId));
        return Ok(result);
    }

    [HttpPost("docks")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateDock(CreateDockCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);    
    }

    [HttpPut("docks/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateDock(Guid id, [FromBody] UpdateDockCommand command)
    {
        if (id != command.DockId) return BadRequest("ID mismatch.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("docks/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDock(Guid id)
    {
        await Mediator.Send(new DeleteDockCommand(id));
        return NoContent();
    }

    [HttpPost("yard-spots")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateYardSpot(CreateYardSpotCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("yard-spots/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateYardSpot(Guid id, [FromBody] UpdateYardSpotCommand command)
    {
        if (id != command.YardSpotId) return BadRequest("ID mismatch.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("yard-spots/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteYardSpot(Guid id)
    {
        await Mediator.Send(new DeleteYardSpotCommand(id));
        return NoContent();
    }
}