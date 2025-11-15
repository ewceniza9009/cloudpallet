using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WMS.Application.Abstractions.Reports;
using WMS.Application.Features.Reports.Queries;
using WMS.Domain.Enums;    
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Reports.Pick;

public class PickReportItem
{
    public DateTime Timestamp { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string MaterialSKU { get; set; } = string.Empty;
    public decimal QuantityPicked { get; set; }
    public decimal WeightPickedKg { get; set; }
    public string SourceLocationBarcode { get; set; } = string.Empty;
    public string PalletBarcode { get; set; } = string.Empty;      
    public string LpnBarcode { get; set; } = string.Empty;      
    public string UserName { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string PickStatus { get; set; } = string.Empty;
}

public static class PickReportGenerator
{
    public static async Task<List<PickReportItem>> GenerateDataAsync(WmsDbContext context, ReportFilterDto filters, CancellationToken cancellationToken)
    {
        var query = context.PickTransactions.AsNoTracking()
            .Where(pt => pt.Timestamp >= filters.StartDate && pt.Timestamp <= filters.EndDate)
            .Where(pt => !filters.AccountId.HasValue || pt.AccountId == filters.AccountId)
            .Where(pt => !filters.UserId.HasValue || pt.UserId == filters.UserId);
        var pickEntries = await query
            .Include(pt => pt.MaterialInventory)
                .ThenInclude(mi => mi.Material)     
            .Include(pt => pt.MaterialInventory)
                .ThenInclude(mi => mi.Location)     
            .Include(pt => pt.MaterialInventory)
                .ThenInclude(mi => mi.Pallet)     
            .Include(pt => pt.User)         
            .Include(pt => pt.Account)     
            .OrderBy(pt => pt.Timestamp)
            .Select(pt => new PickReportItem
            {
                Timestamp = pt.Timestamp,
                MaterialName = pt.MaterialInventory.Material.Name,
                MaterialSKU = pt.MaterialInventory.Material.Sku,
                QuantityPicked = pt.Quantity,
                WeightPickedKg = pt.PickWeight,      
                SourceLocationBarcode = pt.MaterialInventory.Location.Barcode,
                PalletBarcode = pt.MaterialInventory.Pallet.Barcode,       
                LpnBarcode = pt.MaterialInventory.Barcode,                
                UserName = pt.User != null ? $"{pt.User.FirstName} {pt.User.LastName}" : "System",
                AccountId = pt.AccountId,
                AccountName = pt.Account != null ? pt.Account.Name : "N/A",
                PickStatus = pt.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        return pickEntries;
    }
}

public static class PickReportDocumentComposer
{
    public static void ComposeContent(IContainer container, List<PickReportItem> items)
    {
        if (!items.Any())
        {
            container.AlignCenter().Text("No picking data found for the selected filters.").Medium();
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(100);     
                columns.RelativeColumn(3);      
                columns.ConstantColumn(50);     
                columns.ConstantColumn(70);     
                columns.RelativeColumn(1.5f);   
                columns.RelativeColumn(1.5f);   
                columns.RelativeColumn(1.5f);   
                columns.RelativeColumn(1.5f);  
                columns.RelativeColumn(2f);    
                columns.ConstantColumn(60);    
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Timestamp").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Material (SKU)").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Qty").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Wgt (Kg)").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Source Loc").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Pallet").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Item LPN").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("User").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Account").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Status").Bold();

            });

            uint index = 0;
            decimal totalQty = 0;
            decimal totalWgt = 0;

            foreach (var item in items)     
            {
                var backgroundColor = index % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                index++;

                table.Cell().Background(backgroundColor).Padding(3).Text(item.Timestamp.ToString("yyyy-MM-dd HH:mm")).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).Text($"{item.MaterialName} ({item.MaterialSKU})").FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).AlignRight().Text(item.QuantityPicked.ToString("N0")).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).AlignRight().Text(item.WeightPickedKg.ToString("N2")).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.SourceLocationBarcode).FontSize(9).FontFamily(Fonts.Consolas);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.PalletBarcode).FontSize(9).FontFamily(Fonts.Consolas);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.LpnBarcode).FontSize(9).FontFamily(Fonts.Consolas);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.UserName).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.AccountName).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).AlignCenter().Text(item.PickStatus).FontSize(8).FontColor(item.PickStatus == "Short" ? Colors.Red.Medium : Colors.Black);

                totalQty += item.QuantityPicked;
                totalWgt += item.WeightPickedKg;
            }

            table.Cell().ColumnSpan(2).PaddingTop(8).AlignRight().Text("Totals:").Bold().FontSize(10);
            table.Cell().PaddingTop(8).AlignRight().Text(totalQty.ToString("N0")).Bold().FontSize(10);
            table.Cell().PaddingTop(8).AlignRight().Text(totalWgt.ToString("N2")).Bold().FontSize(10);
            table.Cell().ColumnSpan(6).PaddingTop(8).Text("").FontSize(10);    

        });
    }
}