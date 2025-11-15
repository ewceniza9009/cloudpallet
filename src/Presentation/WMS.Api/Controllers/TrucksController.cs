using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Features.WarehouseSetup.Commands;
using WMS.Application.Features.WarehouseSetup.Queries;

namespace WMS.Api.Controllers;

[ApiController]
[Route("api/setup/trucks")]
[Authorize(Policy = "AdminPolicy")]
public class TrucksController : ApiControllerBase
{
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTruck(Guid id, [FromBody] UpdateTruckCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("ID mismatch in route and body.");
        }
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTruck(Guid id)
    {
        await Mediator.Send(new DeleteTruckCommand(id));
        return NoContent();
    }
}