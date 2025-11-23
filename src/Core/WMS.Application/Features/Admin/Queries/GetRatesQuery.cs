using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models;

namespace WMS.Application.Features.Admin.Queries;

public class GetRatesQuery : IRequest<PagedResult<RateDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
    public string? SearchTerm { get; set; }
}

public class GetRatesQueryHandler(IRateRepository rateRepository)
    : IRequestHandler<GetRatesQuery, PagedResult<RateDto>>
{
    public async Task<PagedResult<RateDto>> Handle(GetRatesQuery request, CancellationToken cancellationToken)
    {
        return await rateRepository.GetPagedListAsync(request, cancellationToken);
    }
}