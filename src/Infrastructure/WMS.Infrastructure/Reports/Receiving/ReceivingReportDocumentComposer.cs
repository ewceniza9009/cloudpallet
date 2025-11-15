// ---- File: src/Infrastructure/WMS.Infrastructure/Reports/Receiving/ReceivingReportDocumentComposer.cs ----

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WMS.Infrastructure.Reports.Receiving;

public static class ReceivingReportDocumentComposer
{
    public static void ComposeContent(IContainer container, List<ReceivingReportItem> items)
    {
        if (!items.Any())
        {
            container.AlignCenter().Text("No receiving data found for the selected filters.").Medium();
            return;
        }

        container.Table(table =>
        {
            // Define Columns
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(75); // Date
                columns.RelativeColumn(2.5f); // Supplier
                columns.RelativeColumn(3);    // Material (SKU)
                columns.ConstantColumn(50); // Quantity
                columns.ConstantColumn(40); // UOM
                columns.ConstantColumn(70); // Weight
            });

            // Header Row Styling
            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Date").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Supplier").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Material (SKU)").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Qty").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignLeft().Text("UOM").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Weight (Kg)").Bold();
            });

            // Group data by Receiving Session
            var groupedByReceiving = items
                .GroupBy(i => new { i.ReceivingId, i.Timestamp, i.SupplierName })
                .OrderBy(g => g.Key.Timestamp);

            decimal reportTotalQuantity = 0;
            decimal reportTotalWeight = 0;

            foreach (var group in groupedByReceiving)
            {
                // Group Header Row
                table.Cell().ColumnSpan(6).Background(Colors.Grey.Lighten5).PaddingLeft(5).PaddingVertical(3).Text(text =>
                {
                    text.Span($"{group.Key.Timestamp:yyyy-MM-dd HH:mm} | ").SemiBold();
                    text.Span(group.Key.SupplierName).Italic();
                    text.Span($" (ID: ...{group.Key.ReceivingId.ToString().Substring(28)})").FontSize(8).FontColor(Colors.Grey.Medium);
                });

                decimal groupTotalQuantity = 0;
                decimal groupTotalWeight = 0;

                // Line Item Rows
                foreach (var item in group.OrderBy(i => i.MaterialName))
                {
                    table.Cell().Padding(3).PaddingLeft(10).Text("").FontSize(9); // Empty cell under Date
                    table.Cell().Padding(3).Text(item.MaterialName).FontSize(9);
                    table.Cell().Padding(3).Text($"({item.MaterialSKU})").FontSize(9).FontColor(Colors.Grey.Darken1);
                    table.Cell().Padding(3).AlignRight().Text(item.Quantity.ToString("N0")).FontSize(9);
                    table.Cell().Padding(3).AlignLeft().Text(item.Uom).FontSize(9);
                    table.Cell().Padding(3).AlignRight().Text(item.WeightKg.ToString("N2")).FontSize(9);

                    groupTotalQuantity += item.Quantity;
                    groupTotalWeight += item.WeightKg;
                }

                // Group Footer Row
                table.Cell().ColumnSpan(3).PaddingTop(2).AlignRight().Text("Session Totals:").SemiBold().FontSize(9);
                table.Cell().PaddingTop(2).AlignRight().Text(groupTotalQuantity.ToString("N0")).SemiBold().FontSize(9);
                table.Cell().PaddingTop(2).Text("").FontSize(9); // Empty UOM cell
                table.Cell().PaddingTop(2).AlignRight().Text(groupTotalWeight.ToString("N2")).SemiBold().FontSize(9);

                reportTotalQuantity += groupTotalQuantity;
                reportTotalWeight += groupTotalWeight;

                // Separator Row
                table.Cell().ColumnSpan(6).PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            }

            // Grand Totals Row
            table.Cell().ColumnSpan(3).PaddingTop(8).AlignRight().Text("Grand Totals:").Bold().FontSize(11);
            table.Cell().PaddingTop(8).AlignRight().Text(reportTotalQuantity.ToString("N0")).Bold().FontSize(11);
            table.Cell().PaddingTop(8).Text("").FontSize(11);
            table.Cell().PaddingTop(8).AlignRight().Text(reportTotalWeight.ToString("N2")).Bold().FontSize(11);
        });
    }
}