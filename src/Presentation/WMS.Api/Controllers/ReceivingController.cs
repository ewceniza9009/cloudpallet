using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Api.Common;
using WMS.Application.Common.Models;
using WMS.Application.Features.Inventory.Commands;
using WMS.Application.Features.Inventory.Queries;

namespace WMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "OperatorPolicy")]
public class ReceivingController : ApiControllerBase
{
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(PagedResult<ReceivingSessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions([FromQuery] GetReceivingSessionsQuery query)
    {
        // The [FromQuery] attribute will automatically map:
        // ?warehouseId=...&page=1&pageSize=20 
        // into the GetReceivingSessionsQuery object.

        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("session/{id:guid}")]
    [ProducesResponseType(typeof(ReceivingSessionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionById(Guid id)
    {
        var result = await Mediator.Send(new GetReceivingSessionByIdQuery(id));
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpPost("session")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateSession(CreateReceivingSessionCommand command)
    {
        var receivingId = await Mediator.Send(command);
        return Ok(receivingId);
    }

    [HttpPost("session/{receivingId:guid}/pallet")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddPallet(Guid receivingId, [FromBody] AddPalletToReceivingCommand command)
    {
        if (receivingId != command.ReceivingId)
        {
            return BadRequest("Mismatched Receiving ID.");
        }
        var palletId = await Mediator.Send(command);
        return Ok(palletId);
    }

    [HttpPost("session/{receivingId:guid}/pallet/{palletId:guid}/line")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddLineToPallet(Guid receivingId, Guid palletId, [FromBody] AddLineRequest request)
    {
        var command = new AddLineToPalletCommand(receivingId, palletId, request.MaterialId);
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpPut("line/{palletLineId:guid}/reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPalletLineStatus(Guid palletLineId)
    {
        await Mediator.Send(new ResetPalletLineStatusCommand(palletLineId));
        return NoContent();
    }

    [HttpDelete("session/{receivingId:guid}/pallet/{palletId:guid}/line/{palletLineId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePalletLine(Guid receivingId, Guid palletId, Guid palletLineId)
    {
        await Mediator.Send(new DeletePalletLineCommand(receivingId, palletId, palletLineId));
        return NoContent();
    }

    [HttpDelete("session/{receivingId:guid}/pallet/{palletId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePallet(Guid receivingId, Guid palletId)
    {
        await Mediator.Send(new DeletePalletCommand(receivingId, palletId));
        return NoContent();
    }

    [HttpPost("process-line")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProcessPalletLine(ProcessPalletLineCommand command)
    {
        var commandWithUser = command with { UserId = User.GetUserId() };
        var barcode = await Mediator.Send(commandWithUser);
        return Ok(barcode);
    }

    [HttpPost("session/{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteSession(Guid id)
    {
        await Mediator.Send(new CompleteReceivingSessionCommand(id));
        return NoContent();
    }
}