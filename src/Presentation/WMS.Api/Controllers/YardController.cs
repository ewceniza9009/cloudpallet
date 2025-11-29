using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Features.Yard.Commands;
using WMS.Application.Features.Yard.Queries;

namespace WMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "OperatorPolicy")]
public class YardController : ApiControllerBase
{
    [HttpGet("appointments/today")]
    public async Task<IActionResult> GetTodaysAppointments(
        [FromQuery] Guid warehouseId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var result = await Mediator.Send(new GetTodaysAppointmentsQuery(warehouseId, startDate, endDate));
        return Ok(result);
    }

    [HttpGet("occupied-spots")]
    public async Task<IActionResult> GetOccupiedSpots([FromQuery] Guid warehouseId)
    {
        var result = await Mediator.Send(new GetOccupiedYardSpotsQuery(warehouseId));
        return Ok(result);
    }

    [HttpPost("check-in")]
    public async Task<IActionResult> CheckInTruck(TruckCheckInCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("move-to-dock")]
    public async Task<IActionResult> MoveToDock(MoveTruckToDockCommand command)
    {
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpPost("vacate-spot")]
    public async Task<IActionResult> VacateSpot([FromBody] VacateYardSpotCommand command)
    {
        await Mediator.Send(command);
        return NoContent();
    }
}