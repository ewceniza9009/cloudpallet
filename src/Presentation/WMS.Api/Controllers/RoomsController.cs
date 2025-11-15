// ---- File: src/Presentation/WMS.Api/Controllers/RoomsController.cs ----

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.Features.LocationSetup.Commands;
using WMS.Application.Features.LocationSetup.Queries;

namespace WMS.Api.Controllers;

[ApiController]
[Route("api/setup/rooms")]
[Authorize(Policy = "AdminPolicy")]
public class RoomsController : ApiControllerBase
{
    // ... (GetAllRooms, GetRoomById, CreateRoom are correct) ...
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RoomDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRooms()
    {
        return Ok(await Mediator.Send(new GetRoomsQuery()));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoomDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoomById(Guid id)
    {
        var result = await Mediator.Send(new GetRoomDetailsQuery(id));
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRoom(CreateRoomCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetRoomById), new { id }, id);
    }

    // --- MODIFIED METHOD ---
    [HttpGet("{id:guid}/locations")]
    [ProducesResponseType(typeof(PagedResult<LocationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocationsForRoom(Guid id, [FromQuery] GetLocationsForRoomQuery query)
    {
        // --- FIX: Assign the 'id' from the route to the query object ---
        var finalQuery = query with { RoomId = id };
        // --- END FIX ---

        var result = await Mediator.Send(finalQuery);
        return Ok(result);
    }
    // --- END MODIFICATION ---

    [HttpPost("{id:guid}/locations-bay")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CreateLocationsInBay(Guid id, [FromBody] CreateLocationsInBayCommand command)
    {
        if (id != command.RoomId)
        {
            return BadRequest("Room ID mismatch.");
        }
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{id:guid}/locations-simple")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSimpleLocation(Guid id, [FromBody] CreateSimpleLocationCommand command)
    {
        if (id != command.RoomId)
        {
            return BadRequest("Room ID mismatch.");
        }
        var locationId = await Mediator.Send(command);
        return Ok(locationId); // Return new ID
    }
}