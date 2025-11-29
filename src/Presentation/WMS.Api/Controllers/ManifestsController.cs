using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Features.Manifests.Queries;
using WMS.Application.Features.Manifests.Commands;

namespace WMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ManifestsController : ApiControllerBase
{
    [HttpGet("by-appointment/{appointmentId:guid}")]
    [ProducesResponseType(typeof(CargoManifestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAppointment(Guid appointmentId)
    {
        var query = new GetManifestByAppointmentQuery(appointmentId);
        var result = await Mediator.Send(query);
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCargoManifestCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }
}