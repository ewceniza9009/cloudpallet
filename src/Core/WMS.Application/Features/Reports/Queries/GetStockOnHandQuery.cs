using MediatR;
using WMS.Application.Abstractions.Persistence;    
using WMS.Application.Common.Models;
namespace WMS.Application.Features.Reports.Queries;

public record StockOnHandDto
{
    public Guid MaterialInventoryId { get; init; }
    public string MaterialName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string PalletBarcode { get; init; } = string.Empty;
    public string LpnBarcode { get; init; } = string.Empty;
    public string BatchNumber { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string Room { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string SupplierName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal Weight { get; init; }
    public DateTime? ExpiryDate { get; init; }
}

public record GetStockOnHandQuery : IRequest<PagedResult<StockOnHandDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
    public Guid? AccountId { get; init; }
    public Guid? MaterialId { get; init; }
    public Guid? SupplierId { get; init; }
    public string? BatchNumber { get; init; }
    public string? Barcode { get; init; }
}

public class GetStockOnHandQueryHandler(IReportRepository reportRepository)     
    : IRequestHandler<GetStockOnHandQuery, PagedResult<StockOnHandDto>>
{
    public async Task<PagedResult<StockOnHandDto>> Handle(GetStockOnHandQuery request, CancellationToken cancellationToken)
    {
        return await reportRepository.GetStockOnHandAsync(request, cancellationToken);
    }
}