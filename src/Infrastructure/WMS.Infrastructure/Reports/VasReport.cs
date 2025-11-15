using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;      
using WMS.Application.Abstractions.Reports;
using WMS.Application.Features.Reports.Queries;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Reports.VAS;

public class VasReportItem
{
    public DateTime Timestamp { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
}

public static class VasReportGenerator
{
    public static async Task<List<VasReportItem>> GenerateDataAsync(WmsDbContext context, ReportFilterDto filters, CancellationToken cancellationToken)
    {
        var query = context.VASTransactions.AsNoTracking()
            .Where(vt => vt.Timestamp >= filters.StartDate && vt.Timestamp <= filters.EndDate)
            .Where(vt => !filters.AccountId.HasValue || vt.AccountId == filters.AccountId)
            .Where(vt => !filters.UserId.HasValue || vt.UserId == filters.UserId);
        var vasEntries = await query
            .Include(vt => vt.User)       
            .Include(vt => vt.Account)   
            .OrderBy(vt => vt.Timestamp)
            .Select(vt => new VasReportItem
            {
                Timestamp = vt.Timestamp,
                ServiceType = vt.ServiceType.ToString(),
                Description = vt.Description ?? "No description provided.",      
                UserName = vt.User != null ? $"{vt.User.FirstName} {vt.User.LastName}" : "System",
                AccountId = vt.AccountId,
                AccountName = vt.Account != null ? vt.Account.Name : "N/A"
            })
            .ToListAsync(cancellationToken);

        return vasEntries;
    }
}

public static class VasReportDocumentComposer
{
    public static void ComposeContent(IContainer container, List<VasReportItem> items)
    {
        if (!items.Any())
        {
            container.AlignCenter().Text("No VAS transaction data found for the selected filters.").Medium();
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(100);     
                columns.ConstantColumn(80);       
                columns.RelativeColumn(1.5f);  
                columns.RelativeColumn(2f);    
                columns.RelativeColumn(4);     
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Timestamp").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Service").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("User").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Account").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Description").Bold();
            });

            uint index = 0;
            foreach (var item in items)     
            {
                var backgroundColor = index % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                index++;

                table.Cell().Background(backgroundColor).Padding(3).Text(item.Timestamp.ToString("yyyy-MM-dd HH:mm")).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.ServiceType).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.UserName).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.AccountName).FontSize(9);
                table.Cell().Background(backgroundColor).Padding(3).Text(item.Description).FontSize(9);
            }

            table.Cell().ColumnSpan(5).PaddingTop(10).AlignRight().Text($"Total VAS Transactions: {items.Count}").SemiBold();
        });
    }
}