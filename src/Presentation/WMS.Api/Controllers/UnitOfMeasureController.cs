using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;   
using WMS.Application.Features.WarehouseSetup.Commands;
using WMS.Application.Features.WarehouseSetup.Queries;

namespace WMS.Api.Controllers;

[ApiController]
[Route("api/setup/unit-of-measures")]
[Authorize(Policy = "AdminPolicy")]
public class UnitOfMeasureSetupController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UnitOfMeasureDto>), StatusCodes.Status200OK)]   
    public async Task<IActionResult> GetAll([FromQuery] GetUnitOfMeasuresQuery query)    
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UnitOfMeasureDto), StatusCodes.Status200OK)]   
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetUnitOfMeasureByIdQuery(id));
        return result is not null ? Ok(result) : NotFound();   
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateUnitOfMeasureCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnitOfMeasureCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteUnitOfMeasureCommand(id));
        return NoContent();
    }
}