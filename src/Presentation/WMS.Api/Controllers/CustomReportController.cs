using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Abstractions.Reports;     
using WMS.Application.Features.Reports.Queries;     
using System.Net.Mime;    

namespace WMS.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]   
public class CustomReportController : ApiControllerBase     
{
    private readonly IReportGenerator _reportGenerator;    

    public CustomReportController(IReportGenerator reportGenerator)
    {
        _reportGenerator = reportGenerator;
    }

    [HttpGet("custom")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/pdf")]    
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCustomReport([FromQuery] ReportFilterDto filters, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            byte[] reportBytes = await _reportGenerator.GenerateReportAsync(filters, cancellationToken);

            var contentDisposition = new ContentDisposition
            {
                FileName = $"WMS_Report_{filters.ReportType}_{DateTime.UtcNow:yyyyMMddHHmm}.pdf",
                Inline = true,    
            };
            Response.Headers.Append("Content-Disposition", contentDisposition.ToString());

            return File(reportBytes, "application/pdf");
        }
        catch (NotImplementedException ex)      
        {
            return BadRequest($"Report type '{filters.ReportType}' is not yet implemented: {ex.Message}");
        }
        catch (Exception ex)      
        {
            return Problem(
                detail: $"An error occurred while generating the report: {ex.Message}",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Report Generation Failed"
            );
        }
    }
}