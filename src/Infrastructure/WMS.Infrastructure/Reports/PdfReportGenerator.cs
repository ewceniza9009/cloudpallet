using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WMS.Application.Abstractions.Reports;
using WMS.Application.Features.Reports.Queries;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Reports.Invoice;      
using WMS.Infrastructure.Reports.Pick;
using WMS.Infrastructure.Reports.Putaway;
using WMS.Infrastructure.Reports.Receiving;
using WMS.Infrastructure.Reports.Shipment;
using WMS.Infrastructure.Reports.Transfer;
using WMS.Infrastructure.Reports.VAS;
using IContainer = QuestPDF.Infrastructure.IContainer;
using IDocumentContainer = QuestPDF.Infrastructure.IDocumentContainer;

namespace WMS.Infrastructure.Reports;

public class PdfReportGenerator(WmsDbContext context) : IReportGenerator
{
    private readonly ReceivingReportGenerator _receivingReportGenerator = new(context);
    private readonly PutawayReportGenerator _putawayReportGenerator = new(context);
    private readonly TransferReportGenerator _transferReportGenerator = new(context);
    public async Task<byte[]> GenerateReportAsync(ReportFilterDto filters, CancellationToken cancellationToken)
    {
        string reportTitle = "Generated Report";
        string description = $"Report Type: {filters.ReportType} | Period: {filters.StartDate:d} to {filters.EndDate:d}";
        List<object> reportData = new();
        Dictionary<Guid, string> uomMap = new();

        switch (filters.ReportType)
        {
            case "Receiving":
                reportTitle = "Receiving Report";
                var receivingResult = await _receivingReportGenerator.GenerateDataAsync(filters, cancellationToken);
                reportData.AddRange(receivingResult.Data);
                uomMap = receivingResult.UomMap;
                break;
            case "Putaway":
                reportTitle = "Putaway Report";
                var putawayData = await _putawayReportGenerator.GenerateDataAsync(filters, cancellationToken);
                reportData.AddRange(putawayData);
                break;
            case "Transfer":
                reportTitle = "Pallet Transfer Report";
                var transferData = await _transferReportGenerator.GenerateDataAsync(filters, cancellationToken);
                reportData.AddRange(transferData);
                break;
            case "VAS":
                reportTitle = "Value-Added Services Report";
                var vasData = await VasReportGenerator.GenerateDataAsync(context, filters, cancellationToken);
                reportData.AddRange(vasData);
                break;
            case "Picking":
                reportTitle = "Picking Report";
                var pickData = await PickReportGenerator.GenerateDataAsync(context, filters, cancellationToken);
                reportData.AddRange(pickData);
                break;
            case "Shipping":     
                reportTitle = "Shipment Report";
                var shipmentData = await ShipmentReportGenerator.GenerateDataAsync(context, filters, cancellationToken);
                reportData.AddRange(shipmentData);
                break;
            case "Invoice":      
                reportTitle = "Invoice Report";
                var invoiceData = await InvoiceReportGenerator.GenerateDataAsync(context, filters, cancellationToken);
                reportData.AddRange(invoiceData);        
                break;

            default:
                reportData.Add($"Data generation for report type '{filters.ReportType}' not yet implemented.");
                break;
        }

        var document = new ReportDocument(reportTitle, description, reportData, filters, uomMap);
        byte[] pdfBytes = document.GeneratePdf();
        return pdfBytes;
    }
}

public class ReportDocument : IDocument
{
    private readonly string _title;
    private readonly string _description;
    private readonly List<object> _data;
    private readonly ReportFilterDto _filters;
    private readonly Dictionary<Guid, string> _uomMap;

    public ReportDocument(string title, string description, List<object> data, ReportFilterDto filters, Dictionary<Guid, string> uomMap)
    {
        _title = title;
        _description = description;
        _data = data;
        _filters = filters;
        _uomMap = uomMap;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        if (_filters.ReportType == "Invoice" && _data.Count == 1)
        {
            container.Page(page => InvoiceReportDocumentComposer.ComposeSingleInvoice(page, (InvoiceReportData)_data.First()));
        }
        else
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(ts => ts.FontSize(10));
                page.Header().Element(ComposeStandardHeader);     
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeStandardFooter);     
            });
        }
    }

    private void ComposeStandardHeader(IContainer container)  
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(innerCol =>
                {
                    innerCol.Item().Text(_title).Bold().FontSize(20).AlignRight();
                    innerCol.Item().Text($"WMS Report").FontSize(9).AlignRight().FontColor(Colors.Grey.Medium);
                    innerCol.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).AlignRight().FontColor(Colors.Grey.Medium);
                });
            });
            col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingVertical(8).Background(Colors.Grey.Lighten4).PaddingHorizontal(10).Column(filterCol =>
            {
                filterCol.Item().Text(text =>
                {
                    text.Span("Report Type: ").SemiBold(); text.Span(_filters.ReportType);
                    text.Span("  |  Date Range: ").SemiBold(); text.Span($"{_filters.StartDate:yyyy-MM-dd} to {_filters.EndDate:yyyy-MM-dd}");
                });
                if (_filters.AccountId.HasValue) filterCol.Item().Text(text => { text.Span("Account ID: ").SemiBold(); text.Span(_filters.AccountId.ToString()); });
                if (_filters.SupplierId.HasValue) filterCol.Item().Text(text => { text.Span("Supplier ID: ").SemiBold(); text.Span(_filters.SupplierId.ToString()); });
                if (_filters.MaterialId.HasValue) filterCol.Item().Text(text => { text.Span("Material ID: ").SemiBold(); text.Span(_filters.MaterialId.ToString()); });
                if (_filters.UserId.HasValue) filterCol.Item().Text(text => { text.Span("User ID: ").SemiBold(); text.Span(_filters.UserId.ToString()); });
            });
            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(10).Column(col =>
        {
            switch (_filters.ReportType)
            {
                case "Receiving":
                    ReceivingReportDocumentComposer.ComposeContent(col.Item(), _data.Cast<ReceivingReportItem>().ToList());
                    break;
                case "Putaway":
                    PutawayReportDocumentComposer.ComposeContent(col.Item(), _data.Cast<PutawayReportItem>().ToList());
                    break;
                case "Transfer":
                    TransferReportDocumentComposer.ComposeContent(col.Item(), _data.Cast<TransferReportItem>().ToList());
                    break;
                case "VAS":
                    VasReportDocumentComposer.ComposeContent(col.Item(), _data.Cast<VasReportItem>().ToList());
                    break;
                case "Picking":
                    PickReportDocumentComposer.ComposeContent(col.Item(), _data.Cast<PickReportItem>().ToList());
                    break;
                case "Shipping":
                    ShipmentReportDocumentComposer.ComposeContent(col.Item(), _data.Cast<ShipmentReportItem>().ToList());
                    break;
                case "Invoice":     
                    InvoiceReportDocumentComposer.ComposeMultipleInvoicesContent(col.Item(), _data.Cast<InvoiceReportData>().ToList());
                    break;

                default:
                    col.Item().Text("No specific content layout defined for this report type.").Italic().FontColor(Colors.Grey.Medium);
                    break;
            }
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
}