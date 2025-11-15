using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WMS.Infrastructure.Reports.Transfer;

public static class TransferReportDocumentComposer
{
    public static void ComposeContent(IContainer container, List<TransferReportItem> items)
    {
        if (!items.Any())
        {
            container.AlignCenter().Text("No transfer data found for the selected filters.").Medium();
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(100);     
                columns.RelativeColumn(1.5f);   
                columns.RelativeColumn(2f);     
                columns.RelativeColumn(2f);     
                columns.RelativeColumn(1.5f);  
                columns.RelativeColumn(2f);    
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Timestamp").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Pallet").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("From Location").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("To Location").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("User").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Account").Bold();
            });

            uint index = 0;     
            foreach (var item in items)     
            {
                var backgroundColor = index % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                index++;

                table.Cell().Background(backgroundColor).Padding(3).Text(item.Timestamp.ToString("yyyy-MM-dd HH:mm")).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.PalletBarcode).FontSize(9).FontFamily(Fonts.Consolas);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.FromLocationBarcode).FontSize(9).FontFamily(Fonts.Consolas);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.ToLocationBarcode).FontSize(9).FontFamily(Fonts.Consolas);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.UserName).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.AccountName).FontSize(9);
            }

            table.Cell().ColumnSpan(6).PaddingTop(10).AlignRight().Text($"Total Transfers: {items.Count}").SemiBold();
        });
    }
}