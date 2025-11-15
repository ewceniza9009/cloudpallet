using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WMS.Application.Abstractions.Reports;
using WMS.Application.Features.Reports.Queries;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Reports.Shipment;

public class ShipmentReportItem
{
    public DateTime Timestamp { get; set; }
    public string ShipmentNumber { get; set; } = string.Empty;   
    public string TruckLicensePlate { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;       
    public string MaterialSKU { get; set; } = string.Empty;
    public decimal QuantityShipped { get; set; }   
    public decimal WeightShippedKg { get; set; }   
    public string UserName { get; set; } = string.Empty;      
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string PickStatus { get; set; } = string.Empty;
}

public static class ShipmentReportGenerator
{
    public static async Task<List<ShipmentReportItem>> GenerateDataAsync(WmsDbContext context, ReportFilterDto filters, CancellationToken cancellationToken)
    {
        var query = context.WithdrawalTransactions.AsNoTracking()
            .Where(wt => wt.Timestamp >= filters.StartDate && wt.Timestamp <= filters.EndDate)
            .Where(wt => !filters.AccountId.HasValue || wt.AccountId == filters.AccountId);
        var shipmentEntries = await query
            .Include(wt => wt.Account)          
            .Include(wt => wt.Appointment)      
                .ThenInclude(a => a!.Truck)     
            .Include(wt => wt.Picks)          
                .ThenInclude(p => p.User)      
            .Include(wt => wt.Picks)
                .ThenInclude(p => p.MaterialInventory)
                    .ThenInclude(mi => mi.Material)       
            .OrderBy(wt => wt.Timestamp)
            .SelectMany(wt => wt.Picks.Select(p => new ShipmentReportItem        
            {
                Timestamp = wt.Timestamp,       
                ShipmentNumber = wt.ShipmentNumber ?? $"WT-{wt.Id.ToString().Substring(0, 8)}",
                TruckLicensePlate = wt.Appointment != null && wt.Appointment.Truck != null ? wt.Appointment.Truck.LicensePlate : "N/A",
                MaterialName = p.MaterialInventory.Material.Name,
                MaterialSKU = p.MaterialInventory.Material.Sku,
                QuantityShipped = p.Quantity,
                WeightShippedKg = p.PickWeight,
                UserName = p.User != null ? $"{p.User.FirstName} {p.User.LastName}" : "System",
                AccountId = wt.AccountId,
                AccountName = wt.Account != null ? wt.Account.Name : "N/A",
            }))
            .ToListAsync(cancellationToken);

        return shipmentEntries;
    }
}

public static class ShipmentReportDocumentComposer
{
    public static void ComposeContent(IContainer container, List<ShipmentReportItem> items)
    {
        if (!items.Any())
        {
            container.AlignCenter().Text("No shipment data found for the selected filters.").Medium();
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(100);     
                columns.RelativeColumn(1.5f);   
                columns.RelativeColumn(1.5f);   
                columns.RelativeColumn(3);      
                columns.ConstantColumn(50);     
                columns.ConstantColumn(70);     
                columns.RelativeColumn(1.5f);   
                columns.RelativeColumn(2f);    
                columns.ConstantColumn(60);    
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Timestamp").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Shipment #").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Truck").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Material (SKU)").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Qty").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Wgt (Kg)").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Picker").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Account").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Status").Bold();
            });

            decimal reportTotalQty = 0;
            decimal reportTotalWgt = 0;
            uint index = 0;

            var groupedByShipment = items
               .GroupBy(i => new { i.ShipmentNumber, i.Timestamp, i.TruckLicensePlate, i.AccountName })
               .OrderBy(g => g.Key.Timestamp);

            foreach (var group in groupedByShipment)
            {
                table.Cell().ColumnSpan(8).Background(Colors.Grey.Lighten5).PaddingLeft(5).PaddingVertical(3).Text(text =>
                {
                    text.Span($"{group.Key.Timestamp:yyyy-MM-dd HH:mm} | ").SemiBold();
                    text.Span($"Shipment: {group.Key.ShipmentNumber} | ").SemiBold();
                    text.Span($"Truck: {group.Key.TruckLicensePlate} | ");
                    text.Span($"Account: {group.Key.AccountName}");
                });

                decimal groupTotalQty = 0;
                decimal groupTotalWgt = 0;

                foreach (var item in group.OrderBy(i => i.MaterialName))
                {
                    var backgroundColor = index % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                    index++;

                    table.Cell().Background(backgroundColor).Padding(3).Text("").FontSize(9);     
                    table.Cell().Background(backgroundColor).Padding(3).Text("").FontSize(9);     
                    table.Cell().Background(backgroundColor).Padding(3).Text("").FontSize(9);     
                    table.Cell().Background(backgroundColor).Padding(3).Text($"{item.MaterialName} ({item.MaterialSKU})").FontSize(9);
                    table.Cell().Background(backgroundColor).Padding(3).AlignRight().Text(item.QuantityShipped.ToString("N0")).FontSize(9);
                    table.Cell().Background(backgroundColor).Padding(3).AlignRight().Text(item.WeightShippedKg.ToString("N2")).FontSize(9);
                    table.Cell().Background(backgroundColor).Padding(3).Text(item.UserName).FontSize(9);
                    table.Cell().Background(backgroundColor).Padding(3).Text(item.AccountName).FontSize(9);      
                    table.Cell().Background(backgroundColor).Padding(3).AlignCenter().Text(item.PickStatus).FontSize(8).FontColor(item.PickStatus == "Short" ? Colors.Red.Medium : Colors.Black);


                    groupTotalQty += item.QuantityShipped;
                    groupTotalWgt += item.WeightShippedKg;
                }

                table.Cell().ColumnSpan(4).PaddingTop(2).AlignRight().Text("Shipment Totals:").SemiBold().FontSize(9);
                table.Cell().PaddingTop(2).AlignRight().Text(groupTotalQty.ToString("N0")).SemiBold().FontSize(9);
                table.Cell().PaddingTop(2).AlignRight().Text(groupTotalWgt.ToString("N2")).SemiBold().FontSize(9);
                table.Cell().ColumnSpan(3).PaddingTop(2).Text("").FontSize(9);   

                reportTotalQty += groupTotalQty;
                reportTotalWgt += groupTotalWgt;

                table.Cell().ColumnSpan(8).PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            }

            table.Cell().ColumnSpan(4).PaddingTop(8).AlignRight().Text("Grand Totals:").Bold().FontSize(11);
            table.Cell().PaddingTop(8).AlignRight().Text(reportTotalQty.ToString("N0")).Bold().FontSize(11);   
            table.Cell().PaddingTop(8).AlignRight().Text(reportTotalWgt.ToString("N2")).Bold().FontSize(11);   
            table.Cell().ColumnSpan(3).PaddingTop(8).Text("").FontSize(11);   

        });
    }
}