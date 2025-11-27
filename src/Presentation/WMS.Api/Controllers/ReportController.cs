using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.Features.Reports.Queries;

namespace WMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "FinancePolicy")]
public class ReportsController : ApiControllerBase
{
    [HttpGet("inventory-ledger")]
    public async Task<IActionResult> GetInventoryLedger([FromQuery] GetInventoryLedgerQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("inventory-ledger/details")]
    public async Task<IActionResult> GetInventoryLedgerDetails([FromQuery] GetInventoryLedgerDetailsQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("inventory-ledger/print")]
    public async Task<IActionResult> PrintInventoryLedger([FromQuery] GetInventoryLedgerQuery query)
    {
        var printQuery = query with
        {
            PageSize = 10000,
            Page = 1
        };

        var result = await Mediator.Send(printQuery);

        return Ok(result);
    }

    [HttpGet("activity-log")]
    [Authorize(Policy = "AdminPolicy")]        
    public async Task<IActionResult> GetActivityLog([FromQuery] GetActivityLogQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("stock-on-hand")]
    [ProducesResponseType(typeof(PagedResult<StockOnHandDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockOnHandReport([FromQuery] GetStockOnHandQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }
}