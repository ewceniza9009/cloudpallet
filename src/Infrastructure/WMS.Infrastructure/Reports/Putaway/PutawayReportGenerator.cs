using Microsoft.EntityFrameworkCore;
using System.Text;
using WMS.Application.Abstractions.Reports;
using WMS.Application.Features.Reports.Queries;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Reports.Putaway;

public class PutawayReportGenerator(WmsDbContext context)
{
    private record TempPutawayData(
        DateTime Timestamp,
        string PalletBarcode,
        string DestinationLocationBarcode,
        string UserName,
        Guid AccountId,
        string AccountName,
        List<TempLineData> Lines      
    );
    private record TempLineData(string Name);


    public async Task<List<PutawayReportItem>> GenerateDataAsync(ReportFilterDto filters, CancellationToken cancellationToken)
    {
        var query = context.PutawayTransactions.AsNoTracking()
            .Where(pt => pt.Timestamp >= filters.StartDate && pt.Timestamp <= filters.EndDate)
            .Where(pt => !filters.AccountId.HasValue || pt.Pallet.AccountId == filters.AccountId)
            .Where(pt => !filters.UserId.HasValue || pt.UserId == filters.UserId);
        var intermediateData = await query
            .Include(pt => pt.Pallet)
                .ThenInclude(p => p.Account)
            .Include(pt => pt.Pallet)
                .ThenInclude(p => p.Lines)
                    .ThenInclude(l => l.Material)
            .Include(pt => pt.Location)
            .Include(pt => pt.User)
            .OrderBy(pt => pt.Timestamp)
            .Select(pt => new TempPutawayData(
                 pt.Timestamp,
                 pt.Pallet.Barcode,
                 pt.Location.Barcode,
                 pt.User != null ? $"{pt.User.FirstName} {pt.User.LastName}" : "System",
                 pt.Pallet.AccountId,
                 pt.Pallet.Account != null ? pt.Pallet.Account.Name : "N/A",
                 pt.Pallet.Lines.Select(l => new TempLineData(l.Material.Name)).ToList()      
            ))
            .ToListAsync(cancellationToken);    

        var putawayEntries = intermediateData
            .Select(tempData =>        
            {
                var summaryBuilder = new StringBuilder();
                if (tempData.Lines.Count > 1)     
                {
                    summaryBuilder.Append($"Mixed: {tempData.Lines.First().Name} + {tempData.Lines.Count - 1} other(s)");
                }
                else if (tempData.Lines.Any())
                {
                    summaryBuilder.Append(tempData.Lines.First().Name);
                }
                else
                {
                    summaryBuilder.Append("Empty Pallet?");
                }

                return new PutawayReportItem
                {
                    Timestamp = tempData.Timestamp,
                    PalletBarcode = tempData.PalletBarcode,
                    MaterialSummary = summaryBuilder.ToString(),
                    DestinationLocationBarcode = tempData.DestinationLocationBarcode,
                    UserName = tempData.UserName,
                    AccountId = tempData.AccountId,
                    AccountName = tempData.AccountName
                };
            })
            .ToList();      

        return putawayEntries;
    }
}