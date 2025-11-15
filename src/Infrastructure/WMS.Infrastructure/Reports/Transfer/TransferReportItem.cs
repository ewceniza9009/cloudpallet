namespace WMS.Infrastructure.Reports.Transfer;

public class TransferReportItem
{
    public DateTime Timestamp { get; set; }
    public string PalletBarcode { get; set; } = string.Empty;
    public string FromLocationBarcode { get; set; } = string.Empty;
    public string ToLocationBarcode { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public Guid AccountId { get; set; }     
    public string AccountName { get; set; } = string.Empty;
}