using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models;

namespace WMS.Application.Features.WarehouseSetup.Queries;

public record UnitOfMeasureDto(
    Guid Id,
    string Name,
    string Symbol);

public record GetUnitOfMeasuresQuery : IRequest<PagedResult<UnitOfMeasureDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
    public string? SearchTerm { get; init; }
}

public class GetUnitOfMeasuresQueryHandler(IUnitOfMeasureRepository uomRepository)
    : IRequestHandler<GetUnitOfMeasuresQuery, PagedResult<UnitOfMeasureDto>>
{
    public async Task<PagedResult<UnitOfMeasureDto>> Handle(GetUnitOfMeasuresQuery request, CancellationToken cancellationToken)
    {
        return await uomRepository.GetPagedListAsync(request, cancellationToken);
    }
}