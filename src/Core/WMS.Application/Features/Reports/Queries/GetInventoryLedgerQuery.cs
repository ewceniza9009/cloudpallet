using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models;

namespace WMS.Application.Features.Reports.Queries;

public record InventoryLedgerLineDto
{
    public DateTime Date { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Document { get; init; } = string.Empty;
    public decimal QuantityIn { get; init; }
    public decimal QuantityOut { get; init; }
    public decimal WeightIn { get; init; }
    public decimal WeightOut { get; init; }
    public decimal RunningBalanceQty { get; set; }
    public decimal RunningBalanceWgt { get; set; }
}

public record InventoryLedgerGroupDto
{
    public Guid MaterialId { get; init; }
    public string MaterialName { get; init; } = string.Empty;
    public decimal TotalQtyIn { get; init; }
    public decimal TotalQtyOut { get; init; }
    public decimal NetQtyChange => TotalQtyIn - TotalQtyOut;
    public decimal TotalWgtIn { get; init; }
    public decimal TotalWgtOut { get; init; }
    public decimal NetWgtChange => TotalWgtIn - TotalWgtOut;
    public List<InventoryLedgerLineDto> Lines { get; set; } = new();
}

public record GetInventoryLedgerQuery : IRequest<PagedResult<InventoryLedgerGroupDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public Guid? AccountId { get; init; }
    public Guid? MaterialId { get; init; }
    public Guid? SupplierId { get; init; }
    public Guid? TruckId { get; init; }
    public Guid? UserId { get; init; }
}

public class GetInventoryLedgerQueryHandler(IReportRepository reportRepository)
    : IRequestHandler<GetInventoryLedgerQuery, PagedResult<InventoryLedgerGroupDto>>
{
    public async Task<PagedResult<InventoryLedgerGroupDto>> Handle(GetInventoryLedgerQuery request, CancellationToken cancellationToken)
    {
        return await reportRepository.GetInventoryLedgerAsync(request, cancellationToken);
    }
}