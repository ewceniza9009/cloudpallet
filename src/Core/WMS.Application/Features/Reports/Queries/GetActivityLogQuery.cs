using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models;

namespace WMS.Application.Features.Reports.Queries;

public record ActivityLogDto
{
    public DateTime Timestamp { get; init; }
    public string User { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Account { get; init; }
}

public record GetActivityLogQuery : IRequest<PagedResult<ActivityLogDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public Guid? AccountId { get; init; }
    public Guid? UserId { get; init; }
    public Guid? TruckId { get; init; }
}

public class GetActivityLogQueryHandler(IReportRepository reportRepository)
    : IRequestHandler<GetActivityLogQuery, PagedResult<ActivityLogDto>>
{
    public async Task<PagedResult<ActivityLogDto>> Handle(GetActivityLogQuery request, CancellationToken cancellationToken)
    {
        return await reportRepository.GetActivityLogAsync(request, cancellationToken);
    }
}