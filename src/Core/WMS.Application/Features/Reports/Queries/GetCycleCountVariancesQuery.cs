using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models;

namespace WMS.Application.Features.Reports.Queries;

public record CycleCountVarianceDto
{
    public Guid AdjustmentId { get; init; }
    public DateTime Timestamp { get; init; }
    public string MaterialName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public string PalletBarcode { get; init; } = string.Empty;
    public decimal VarianceQuantity { get; init; }
    public decimal VarianceValue { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
}

public record GetCycleCountVariancesQuery : IRequest<PagedResult<CycleCountVarianceDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public Guid? AccountId { get; init; }
    public Guid? MaterialId { get; init; }
}

public class GetCycleCountVariancesQueryHandler(IReportRepository reportRepository)
    : IRequestHandler<GetCycleCountVariancesQuery, PagedResult<CycleCountVarianceDto>>
{
    public async Task<PagedResult<CycleCountVarianceDto>> Handle(GetCycleCountVariancesQuery request, CancellationToken cancellationToken)
    {
        return await reportRepository.GetCycleCountVariancesAsync(request, cancellationToken);
    }
}
