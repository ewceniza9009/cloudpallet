using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WMS.Application.Abstractions.Reports;
using WMS.Application.Features.Reports.Queries;
using WMS.Domain.Enums;      
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Reports.Invoice;

public class InvoiceReportHeaderDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime DueDate { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class InvoiceReportLineDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitRate { get; set; }
    public decimal Amount { get; set; }
    public ServiceType ServiceType { get; set; }
    public string Tier { get; set; } = string.Empty;
}

public class InvoiceReportSummaryDto
{
    public ServiceType ServiceType { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

public class InvoiceReportData
{
    public InvoiceReportHeaderDto Header { get; set; } = null!;
    public List<InvoiceReportLineDto> Lines { get; set; } = new();
    public List<InvoiceReportSummaryDto> Summary { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public static class InvoiceReportGenerator
{
    public static async Task<InvoiceReportData?> GenerateDataAsync(WmsDbContext context, Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await context.Invoices.AsNoTracking()
            .Where(inv => inv.Id == invoiceId)
            .Include(inv => inv.Account)
            .Include(inv => inv.Lines)
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice == null)
        {
            return null;
        }

        var lines = invoice.Lines
            .OrderBy(l => l.ServiceType)
            .ThenBy(l => l.Description)
            .Select(l => new InvoiceReportLineDto
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitRate = l.UnitRate,
                Amount = l.Amount,
                ServiceType = l.ServiceType,
                Tier = l.Tier
            }).ToList();

        var subtotal = lines.Sum(l => l.Amount);

        decimal taxAmount;
        decimal totalAmount;
        if (invoice.Status == InvoiceStatus.Draft)
        {
            taxAmount = subtotal * 0.12m;    
            totalAmount = subtotal + taxAmount;
        }
        else
        {
            taxAmount = invoice.TaxAmount;
            totalAmount = invoice.TotalAmount;
        }

        var summary = lines
            .GroupBy(l => l.ServiceType)
            .Select(g => new InvoiceReportSummaryDto
            {
                ServiceType = g.Key,
                ServiceTypeName = g.Key.ToString(),
                TotalAmount = g.Sum(l => l.Amount)
            })
            .OrderBy(s => s.ServiceTypeName)
            .ToList();

        return new InvoiceReportData
        {
            Header = new InvoiceReportHeaderDto
            {
                InvoiceNumber = invoice.InvoiceNumber,
                PeriodStart = invoice.PeriodStart,
                PeriodEnd = invoice.PeriodEnd,
                DueDate = invoice.DueDate,
                AccountName = invoice.Account?.Name ?? "N/A",
                Status = invoice.Status.ToString()
            },
            Lines = lines,
            Summary = summary,
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount
        };
    }

    public static async Task<List<InvoiceReportData>> GenerateDataAsync(WmsDbContext context, ReportFilterDto filters, CancellationToken cancellationToken)
    {
        var query = context.Invoices.AsNoTracking()
            .Where(inv => inv.PeriodStart >= filters.StartDate && inv.PeriodEnd <= filters.EndDate)
            .Where(inv => !filters.AccountId.HasValue || inv.AccountId == filters.AccountId);
        var invoices = await query
            .Include(inv => inv.Account)
            .Include(inv => inv.Lines)
            .OrderBy(inv => inv.PeriodStart)
            .ThenBy(inv => inv.Account.Name)
            .ToListAsync(cancellationToken);

        var reportDataList = new List<InvoiceReportData>();

        foreach (var invoice in invoices)
        {
            var lines = invoice.Lines
                .OrderBy(l => l.ServiceType)
                .ThenBy(l => l.Description)
                .Select(l => new InvoiceReportLineDto
                {
                    Description = l.Description,
                    Quantity = l.Quantity,
                    UnitRate = l.UnitRate,
                    Amount = l.Amount,
                    ServiceType = l.ServiceType,
                    Tier = l.Tier
                }).ToList();

            var subtotal = lines.Sum(l => l.Amount);

            decimal taxAmount;
            decimal totalAmount;
            if (invoice.Status == InvoiceStatus.Draft)
            {
                taxAmount = subtotal * 0.12m;    
                totalAmount = subtotal + taxAmount;
            }
            else
            {
                taxAmount = invoice.TaxAmount;
                totalAmount = invoice.TotalAmount;
            }


            var summary = lines
                .GroupBy(l => l.ServiceType)
                .Select(g => new InvoiceReportSummaryDto
                {
                    ServiceType = g.Key,
                    ServiceTypeName = g.Key.ToString(),    
                    TotalAmount = g.Sum(l => l.Amount)
                })
                .OrderBy(s => s.ServiceTypeName)
                .ToList();

            reportDataList.Add(new InvoiceReportData
            {
                Header = new InvoiceReportHeaderDto
                {
                    InvoiceNumber = invoice.InvoiceNumber,
                    PeriodStart = invoice.PeriodStart,
                    PeriodEnd = invoice.PeriodEnd,
                    DueDate = invoice.DueDate,
                    AccountName = invoice.Account?.Name ?? "N/A",
                    Status = invoice.Status.ToString()
                },
                Lines = lines,
                Summary = summary,
                Subtotal = subtotal,       
                TaxAmount = taxAmount,      
                TotalAmount = totalAmount      
            });
        }

        return reportDataList;
    }
}

public static class InvoiceReportDocumentComposer
{
    private static CultureInfo cultureInfo = new CultureInfo("en-PH");

    public static void ComposeSingleInvoice(PageDescriptor page, InvoiceReportData invoiceData)
    {
        page.Size(PageSizes.A4);
        page.Margin(30);
        page.DefaultTextStyle(ts => ts.FontSize(10));
        page.Header().Element(container => ComposeInvoiceHeader(container, invoiceData.Header));
        page.Content().Element(container => ComposeInvoiceContent(container, invoiceData));
        page.Footer().Element(ComposeStandardFooter);
    }

    private static void ComposeInvoiceHeader(IContainer container, InvoiceReportHeaderDto header)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(innerCol =>
                {
                    innerCol.Item().Text($"Invoice #{header.InvoiceNumber}").Bold().FontSize(18);
                    innerCol.Item().Text($"Account: {header.AccountName}").SemiBold();
                    innerCol.Item().Text($"Billing Period: {header.PeriodStart:yyyy-MM-dd} to {header.PeriodEnd:yyyy-MM-dd}");
                    innerCol.Item().Text($"Due Date: {header.DueDate:yyyy-MM-dd}");
                    innerCol.Item().Text($"Status: {header.Status}");
                });
            });
            col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private static void ComposeInvoiceContent(IContainer container, InvoiceReportData invoiceData)
    {
        container.PaddingTop(10).Column(col =>
        {
            col.Item().Section("Line Items").Table(table =>      
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);  
                    columns.ConstantColumn(60);   
                    columns.ConstantColumn(80);    
                    columns.ConstantColumn(90);   
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Description").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Quantity").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Unit Rate").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Amount").Bold();
                });

                uint index = 0;
                foreach (var line in invoiceData.Lines)
                {
                    var backgroundColor = index % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                    index++;

                    table.Cell().Background(backgroundColor).Padding(3).Text(line.Description).FontSize(9);
                    table.Cell().Background(backgroundColor).Padding(3).AlignRight().Text(line.Quantity.ToString("N2")).FontSize(9);
                    table.Cell().Background(backgroundColor).Padding(3).AlignRight().Text(line.UnitRate.ToString("C2", cultureInfo)).FontSize(9);   
                    table.Cell().Background(backgroundColor).Padding(3).AlignRight().Text(line.Amount.ToString("C2", cultureInfo)).FontSize(9).SemiBold();
                }
            });

            col.Item().PaddingTop(15).AlignRight().Column(totalsCol =>
            {
                totalsCol.Spacing(2);
                totalsCol.Item().Row(row =>
                {
                    row.RelativeItem().Text("Subtotal:");
                    row.ConstantItem(100).AlignRight().Text(invoiceData.Subtotal.ToString("C2", cultureInfo));
                });
                totalsCol.Item().Row(row =>
                {
                    row.RelativeItem().Text("Tax:");
                    row.ConstantItem(100).AlignRight().Text(invoiceData.TaxAmount.ToString("C2", cultureInfo));
                });
                totalsCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                totalsCol.Item().Row(row =>
                {
                    row.RelativeItem().Text("Total Amount Due:").Bold();
                    row.ConstantItem(100).AlignRight().Text(invoiceData.TotalAmount.ToString("C2", cultureInfo)).Bold();
                });
            });

            col.Item().PaddingTop(20).Section("Summary by Service Type").Table(summaryTable =>   
            {
                summaryTable.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);    
                    columns.ConstantColumn(100);   
                });

                summaryTable.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Service Type").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Total Amount").Bold();
                });

                uint summaryIndex = 0;
                foreach (var summaryLine in invoiceData.Summary)
                {
                    var backgroundColor = summaryIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                    summaryIndex++;
                    summaryTable.Cell().Background(backgroundColor).Padding(3).Text(summaryLine.ServiceTypeName).FontSize(9);
                    summaryTable.Cell().Background(backgroundColor).Padding(3).AlignRight().Text(summaryLine.TotalAmount.ToString("C2", cultureInfo)).FontSize(9);
                }

                summaryTable.Cell().PaddingTop(5).AlignRight().Text("Summary Total:").Bold();
                summaryTable.Cell().PaddingTop(5).AlignRight().Text(invoiceData.Summary.Sum(s => s.TotalAmount).ToString("C2", cultureInfo)).Bold();
            });
        });
    }

    private static void ComposeStandardFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Page ");
            x.CurrentPageNumber();
            x.Span(" of ");
            x.TotalPages();
        });
    }

    public static void ComposeMultipleInvoicesContent(IContainer container, List<InvoiceReportData> invoiceList)
    {
        if (!invoiceList.Any())
        {
            container.AlignCenter().Text("No invoices found for the selected filters.").Medium();
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn();   
                cols.RelativeColumn(2);  
                cols.ConstantColumn(90);   
                cols.ConstantColumn(90);   
                cols.ConstantColumn(100);  
                cols.ConstantColumn(80);  
            });

            table.Header(h =>
            {
                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Invoice #").Bold();
                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Account").Bold();
                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Period Start").Bold();
                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Period End").Bold();
                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Total").Bold();
                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Status").Bold();
            });

            uint index = 0;
            foreach (var invoiceData in invoiceList)
            {
                var backgroundColor = index % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                index++;
                table.Cell().Background(backgroundColor).Padding(3).Text(invoiceData.Header.InvoiceNumber).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).Text(invoiceData.Header.AccountName).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).Text(invoiceData.Header.PeriodStart.ToString("yyyy-MM-dd")).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).Text(invoiceData.Header.PeriodEnd.ToString("yyyy-MM-dd")).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).AlignRight().Text(invoiceData.TotalAmount.ToString("C2", cultureInfo)).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).AlignCenter().Text(invoiceData.Header.Status).FontSize(9);
            }

            var overallTotal = invoiceList.Sum(inv => inv.TotalAmount);
            table.Cell().ColumnSpan(4).PaddingTop(8).AlignRight().Text("Overall Total:").Bold();
            table.Cell().PaddingTop(8).AlignRight().Text(overallTotal.ToString("C2", cultureInfo)).Bold();
            table.Cell().PaddingTop(8).Text("");      
        });
    }
}