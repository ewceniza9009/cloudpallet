using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Features.Dock.Appointments.Commands;
using WMS.Application.Features.Dock.Appointments.Queries;

namespace WMS.Api.Controllers;

[Authorize(Policy = "OperatorPolicy")]
public class DockAppointmentsController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Schedule([FromBody] ScheduleDockAppointmentCommand command)
    {
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result }, result);
    }

    [HttpGet("/api/docks/{dockId:guid}/appointments")]
    [ProducesResponseType(typeof(IEnumerable<DockAppointmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForDock(Guid dockId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var query = new GetDockAppointmentsQuery(dockId, startDate, endDate);
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("/api/docks/appointments")]
    [ProducesResponseType(typeof(IEnumerable<DockAppointmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForWarehouse([FromQuery] Guid warehouseId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var query = new GetWarehouseAppointmentsQuery(warehouseId, startDate, endDate);
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)  
    {
        var result = await Mediator.Send(new GetAppointmentDetailsQuery(id));
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<DockAppointmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchByPlate([FromQuery] string licensePlate)
    {
        if (string.IsNullOrWhiteSpace(licensePlate))
        {
            return Ok(Enumerable.Empty<DockAppointmentDto>());
        }
        var result = await Mediator.Send(new GetAppointmentsByPlateQuery(licensePlate));
        return Ok(result);
    }


    [HttpPost("{dockId:guid}/start-unloading")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> StartUnloading(Guid dockId)
    {
        var receivingSessionId = await Mediator.Send(new StartUnloadingCommand(dockId));
        return Ok(receivingSessionId);
    }


    [HttpPost("{dockId:guid}/vacate")]   
    public async Task<IActionResult> VacateDock(Guid dockId)
    {
        await Mediator.Send(new VacateDockCommand(dockId));
        return NoContent();
    }

    [HttpPut("appointments/{id:guid}/reschedule")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reschedule(Guid id, [FromBody] RescheduleAppointmentCommand command)
    {
        if (id != command.AppointmentId)
        {
            return BadRequest("ID mismatch");
        }
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await Mediator.Send(new CancelAppointmentCommand(id));
        return NoContent();
    }
}