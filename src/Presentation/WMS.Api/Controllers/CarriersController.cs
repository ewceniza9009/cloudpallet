using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Features.WarehouseSetup.Commands;
using WMS.Application.Features.WarehouseSetup.Queries;

namespace WMS.Api.Controllers;

[ApiController]
[Route("api/setup/carriers")]
[Authorize(Policy = "AdminPolicy")]
public class CarriersController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CarrierDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await Mediator.Send(new GetCarriersQuery()));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CarrierDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetCarrierByIdQuery(id));
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateCarrierCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCarrierCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpGet("{id:guid}/trucks")]
    [ProducesResponseType(typeof(IEnumerable<TruckDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrucksByCarrier(Guid id)
    {
        return Ok(await Mediator.Send(new GetTrucksByCarrierQuery(id)));
    }

    [HttpPost("{id:guid}/trucks")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTruck(Guid id, [FromBody] CreateTruckCommand command)
    {
        if (id != command.CarrierId) return BadRequest("Carrier ID mismatch.");
        var truckId = await Mediator.Send(command);
        return Ok(truckId);       
    }
}