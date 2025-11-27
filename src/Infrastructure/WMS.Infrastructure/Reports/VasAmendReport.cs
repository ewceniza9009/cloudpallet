using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WMS.Application.Abstractions.Reports;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Reports.VAS;

public class VasAmendReportItem
{
    public DateTime Timestamp { get; set; }
    public string OriginalTransactionDescription { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string AmendmentType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
}

public static class VasAmendReportGenerator
{
    public static async Task<List<VasAmendReportItem>> GenerateDataAsync(WmsDbContext context, ReportFilterDto filters, CancellationToken cancellationToken)
    {
        var query = context.VASTransactionAmendments.AsNoTracking()
            .Where(a => a.Timestamp >= filters.StartDate && a.Timestamp <= filters.EndDate)
            .Where(a => !filters.AccountId.HasValue || a.OriginalTransaction.AccountId == filters.AccountId)
            .Where(a => !filters.UserId.HasValue || a.UserId == filters.UserId);

        var amendments = await query
            .Include(a => a.User)
            .Include(a => a.OriginalTransaction)
                .ThenInclude(t => t.Account)
            .OrderBy(a => a.Timestamp)
            .Select(a => new VasAmendReportItem
            {
                Timestamp = a.Timestamp,
                OriginalTransactionDescription = a.OriginalTransaction.Description,
                UserName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}" : "System",
                AmendmentType = a.AmendmentType.ToString(),
                Reason = a.Reason,
                Details = a.AmendmentDetails,
                AccountId = a.OriginalTransaction.AccountId,
                AccountName = a.OriginalTransaction.Account != null ? a.OriginalTransaction.Account.Name : "N/A"
            })
            .ToListAsync(cancellationToken);

        return amendments;
    }
}

public static class VasAmendReportDocumentComposer
{
    public static void ComposeContent(IContainer container, List<VasAmendReportItem> items)
    {
        if (!items.Any())
        {
            container.AlignCenter().Text("No VAS amendment data found for the selected filters.").Medium();
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(90);      // Timestamp
                columns.RelativeColumn(1.5f);    // User
                columns.RelativeColumn(1.5f);    // Type
                columns.RelativeColumn(2f);      // Reason
                columns.RelativeColumn(3f);      // Details
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Timestamp").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("User").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Type").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Reason").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Details").Bold();
            });

            uint index = 0;
            foreach (var item in items)
            {
                var backgroundColor = index % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                index++;

                table.Cell().Background(backgroundColor).Padding(3).Text(item.Timestamp.ToString("yyyy-MM-dd HH:mm")).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.UserName).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.AmendmentType).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.Reason).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.Details).FontSize(8).FontColor(Colors.Grey.Darken2); // Smaller font for details
            }

            table.Cell().ColumnSpan(5).PaddingTop(10).AlignRight().Text($"Total Amendments: {items.Count}").SemiBold();
        });
    }
}
