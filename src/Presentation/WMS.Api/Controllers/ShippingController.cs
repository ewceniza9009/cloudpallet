using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Features.Shipments.Commands;
using WMS.Application.Features.Shipments.Queries;

namespace WMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "OperatorPolicy")]       
public class ShippingController : ApiControllerBase
{
    [HttpGet("ready-to-ship")]
    [ProducesResponseType(typeof(IEnumerable<ShippableGroupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReadyToShip([FromQuery] Guid warehouseId)
    {
        var result = await Mediator.Send(new GetShippablePicksQuery(warehouseId));
        return Ok(result);
    }

    [HttpPost("ship")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ShipGoods(ShipGoodsCommand command)
    {
        var withdrawalId = await Mediator.Send(command);
        return Ok(withdrawalId);
    }
}