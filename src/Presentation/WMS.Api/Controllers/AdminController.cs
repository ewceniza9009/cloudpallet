using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.Features.Admin.Commands;
using WMS.Application.Features.Admin.Queries;
using WMS.Domain.Enums;

namespace WMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminPolicy")]
public class AdminController : ApiControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await Mediator.Send(new GetUsersQuery());
        return Ok(users);
    }

    [HttpPut("users/{userId:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UserRole newRole)
    {
        await Mediator.Send(new UpdateUserRoleCommand(userId, newRole));
        return NoContent();
    }

    [HttpGet("rates")]
    [Authorize(Policy = "FinancePolicy")]
    [ProducesResponseType(typeof(PagedResult<RateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRates([FromQuery] GetRatesQuery query)
    {
        return Ok(await Mediator.Send(query));
    }

    [HttpPost("rates")]
    [Authorize(Policy = "FinancePolicy")]
    public async Task<IActionResult> CreateRate(CreateRateCommand command)
    {
        var rateId = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetRates), new { id = rateId }, rateId);
    }

    [HttpGet("rates/{id:guid}")]
    [Authorize(Policy = "FinancePolicy")]
    [ProducesResponseType(typeof(RateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRateById(Guid id)
    {
        var rate = await Mediator.Send(new GetRateByIdQuery(id));
        return rate is not null ? Ok(rate) : NotFound();
    }

    [HttpPut("rates/{id:guid}")]
    [Authorize(Policy = "FinancePolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRate(Guid id, UpdateRateCommand command)
    {
        if (id != command.Id) return BadRequest();
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("rates/{id:guid}")]
    [Authorize(Policy = "FinancePolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRate(Guid id)
    {
        await Mediator.Send(new DeleteRateCommand(id));
        return NoContent();
    }

    [HttpGet("materials")]
    [ProducesResponseType(typeof(PagedResult<MaterialDetailDto>), StatusCodes.Status200OK)]     
    public async Task<IActionResult> GetMaterials([FromQuery] GetMaterialsQuery query)     
    {
        var materialsResult = await Mediator.Send(query);
        return Ok(materialsResult);
    }

    [HttpGet("materials/lookup")]
    [ProducesResponseType(typeof(IEnumerable<MaterialDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMaterialLookup()
    {
        var materials = await Mediator.Send(new GetMaterialLookupQuery());
        return Ok(materials);
    }

    [HttpPost("materials")]
    public async Task<IActionResult> CreateMaterial(CreateMaterialCommand command)
    {
        var materialId = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetMaterials), new { id = materialId }, materialId);
    }

    [HttpGet("materials/{id:guid}")]
    [ProducesResponseType(typeof(MaterialDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMaterialById(Guid id)
    {
        var material = await Mediator.Send(new GetMaterialByIdQuery(id));
        return material is not null ? Ok(material) : NotFound();
    }

    [HttpPut("materials/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMaterial(Guid id, UpdateMaterialCommand command)
    {
        if (id != command.Id) return BadRequest();
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("materials/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMaterial(Guid id)
    {
        await Mediator.Send(new DeleteMaterialCommand(id));
        return NoContent();
    }

    [HttpPost("boms")]
    public async Task<IActionResult> CreateBillOfMaterial(CreateBillOfMaterialCommand command)
    {
        var bomId = await Mediator.Send(command);
        return Ok(bomId);
    }

    [HttpGet("materials/{outputMaterialId:guid}/bom")]
    [ProducesResponseType(typeof(BomDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBomByOutputMaterialId(Guid outputMaterialId)
    {
        var bom = await Mediator.Send(new GetBomByOutputMaterialIdQuery(outputMaterialId));
        return bom is not null ? Ok(bom) : NotFound();
    }
}