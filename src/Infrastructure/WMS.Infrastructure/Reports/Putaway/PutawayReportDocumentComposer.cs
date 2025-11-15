using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WMS.Infrastructure.Reports.Putaway;

public static class PutawayReportDocumentComposer
{
    public static void ComposeContent(IContainer container, List<PutawayReportItem> items)
    {
        if (!items.Any())
        {
            container.AlignCenter().Text("No putaway data found for the selected filters.").Medium();
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(100);     
                columns.RelativeColumn(1.5f);   
                columns.RelativeColumn(3);       
                columns.RelativeColumn(1.5f);  
                columns.RelativeColumn(1.5f);  
                columns.RelativeColumn(2f);    
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Timestamp").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Pallet").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Contents").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Destination").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("User").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Account").Bold();
            });

            foreach (var item in items)     
            {
                table.Cell().Padding(3).Text(item.Timestamp.ToString("yyyy-MM-dd HH:mm")).FontSize(9);
                table.Cell().Padding(3).Text(item.PalletBarcode).FontSize(9).FontFamily(Fonts.Consolas);    
                table.Cell().Padding(3).Text(item.MaterialSummary).FontSize(9);
                table.Cell().Padding(3).Text(item.DestinationLocationBarcode).FontSize(9).FontFamily(Fonts.Consolas);
                table.Cell().Padding(3).Text(item.UserName).FontSize(9);
                table.Cell().Padding(3).Text(item.AccountName).FontSize(9);
            }

            table.Cell().ColumnSpan(6).PaddingTop(10).AlignRight().Text($"Total Putaways: {items.Count}").SemiBold();
        });
    }
}