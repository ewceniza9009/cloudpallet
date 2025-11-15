using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;   
using WMS.Application.Abstractions.Reports;
using WMS.Application.Features.Billing.Commands;
using WMS.Application.Features.Billing.Queries;
using WMS.Infrastructure.Persistence;    
using WMS.Infrastructure.Reports;    
using WMS.Infrastructure.Reports.Invoice;    
using QuestPDF.Fluent;

namespace WMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "FinancePolicy")]
public class BillingController : ApiControllerBase
{
    private readonly WmsDbContext _context;
    private readonly IReportGenerator _reportGenerator;       

    public BillingController(WmsDbContext context, IReportGenerator reportGenerator)
    {
        _context = context;
        _reportGenerator = reportGenerator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoices([FromQuery] Guid accountId)
    {
        var result = await Mediator.Send(new GetInvoicesByAccountQuery(accountId));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoiceById(Guid id)
    {
        var result = await Mediator.Send(new GetInvoiceByIdQuery(id));
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> GenerateInvoice(GenerateInvoiceCommand command)
    {
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetInvoiceById), new { id = result }, result);
    }

    [HttpGet("{id:guid}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoicePdf(Guid id, CancellationToken cancellationToken)
    {
        var invoiceData = await InvoiceReportGenerator.GenerateDataAsync(_context, id, cancellationToken);
        if (invoiceData == null)
        {
            return NotFound($"Invoice with ID {id} not found.");
        }

        var filters = new ReportFilterDto
        {
            ReportType = "Invoice",
        };

        var dataList = new List<object> { invoiceData };

        var document = new ReportDocument(
            $"Invoice #{invoiceData.Header.InvoiceNumber}",
            $"Invoice for {invoiceData.Header.AccountName}",
            dataList,
            filters,
            new Dictionary<Guid, string>()      
        );

        byte[] pdfBytes = document.GeneratePdf();

        var contentDisposition = new ContentDisposition
        {
            FileName = $"Invoice_{invoiceData.Header.InvoiceNumber}.pdf",
            Inline = true,    
        };
        Response.Headers.Append("Content-Disposition", contentDisposition.ToString());

        return File(pdfBytes, "application/pdf");
    }
}