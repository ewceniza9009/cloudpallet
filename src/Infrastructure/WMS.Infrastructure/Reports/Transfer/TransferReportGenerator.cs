using Microsoft.EntityFrameworkCore;
using WMS.Application.Abstractions.Reports;
using WMS.Application.Features.Reports.Queries;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Reports.Transfer;

public class TransferReportGenerator(WmsDbContext context)
{
    public async Task<List<TransferReportItem>> GenerateDataAsync(ReportFilterDto filters, CancellationToken cancellationToken)
    {
        var query = context.TransferTransactions.AsNoTracking()
            .Where(tt => tt.Timestamp >= filters.StartDate && tt.Timestamp <= filters.EndDate)
            .Where(tt => !filters.AccountId.HasValue || tt.Pallet.AccountId == filters.AccountId)
            .Where(tt => !filters.UserId.HasValue || tt.UserId == filters.UserId);
        var transferEntries = await query
            .Include(tt => tt.Pallet)
                .ThenInclude(p => p.Account)     
            .Include(tt => tt.FromLocation)    
            .Include(tt => tt.ToLocation)      
            .Include(tt => tt.User)         
            .OrderBy(tt => tt.Timestamp)
            .Select(tt => new TransferReportItem
            {
                Timestamp = tt.Timestamp,
                PalletBarcode = tt.Pallet.Barcode,
                FromLocationBarcode = tt.FromLocation.Barcode,
                ToLocationBarcode = tt.ToLocation.Barcode,
                UserName = tt.User != null ? $"{tt.User.FirstName} {tt.User.LastName}" : "System",
                AccountId = tt.Pallet.AccountId,
                AccountName = tt.Pallet.Account != null ? tt.Pallet.Account.Name : "N/A"
            })
            .ToListAsync(cancellationToken);

        return transferEntries;
    }
}