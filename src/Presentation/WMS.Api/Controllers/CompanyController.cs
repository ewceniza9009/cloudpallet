using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Features.Companies.Commands;
using WMS.Application.Features.Companies.Queries;

namespace WMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminPolicy")]       
public class CompanyController : ApiControllerBase      
{
    [HttpGet]
    [ProducesResponseType(typeof(CompanyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompanyDetails()
    {
        var result = await Mediator.Send(new GetCompanyDetailsQuery());
        return result is not null ? Ok(result) : NotFound("Company details not found.");
    }

    [HttpPut]            
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCompanyDetails(UpdateCompanyCommand command)
    {
        await Mediator.Send(command);
        return NoContent();
    }
}