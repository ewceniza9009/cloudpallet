using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Abstractions.Integrations;
using WMS.Domain.ValueObjects;

namespace WMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "OperatorPolicy")]
public class ScaleController : ApiControllerBase
{
    private readonly IScaleApiService _scaleApiService;

    public ScaleController(IScaleApiService scaleApiService)
    {
        _scaleApiService = scaleApiService;
    }

    [HttpGet("weight")]
    [ProducesResponseType(typeof(Weight), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentWeight(CancellationToken cancellationToken)
    {
        var weight = await _scaleApiService.GetCurrentWeightAsync(cancellationToken);
        return Ok(weight);
    }
}