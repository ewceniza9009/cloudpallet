using WMS.Application.Features.Reports.Queries;      

namespace WMS.Application.Abstractions.Reports;

public record ReportFilterDto
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? MaterialId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? UserId { get; set; }
}

public interface IReportGenerator
{
    Task<byte[]> GenerateReportAsync(ReportFilterDto filters, CancellationToken cancellationToken);
}