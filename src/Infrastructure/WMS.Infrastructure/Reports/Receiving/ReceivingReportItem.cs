namespace WMS.Infrastructure.Reports.Receiving;

public class ReceivingReportItem
{
    public Guid ReceivingId { get; set; }
    public DateTime Timestamp { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public Guid MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string MaterialSKU { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public Guid UomId { get; set; }
    public string Uom { get; set; } = string.Empty;    
    public decimal WeightKg { get; set; }
}