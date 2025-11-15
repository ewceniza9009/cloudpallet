using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Reports.Putaway;

public class PutawayReportItem
{
    public DateTime Timestamp { get; set; }
    public string PalletBarcode { get; set; } = string.Empty;
    public string MaterialSummary { get; set; } = string.Empty;     
    public string DestinationLocationBarcode { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public Guid AccountId { get; set; }     
    public string AccountName { get; set; } = string.Empty;
}