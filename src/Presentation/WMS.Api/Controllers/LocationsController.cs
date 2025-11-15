using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Features.LocationSetup.Commands;
namespace WMS.Api.Controllers;

[ApiController]
[Route("api/setup/locations")]       
[Authorize(Policy = "AdminPolicy")]
public class LocationsController : ApiControllerBase
{
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] UpdateLocationCommand command)
    {
        if (id != command.LocationId)
        {
            return BadRequest("ID mismatch in route and body.");
        }
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLocation(Guid id)
    {
        await Mediator.Send(new DeleteLocationCommand(id));
        return NoContent();
    }
}