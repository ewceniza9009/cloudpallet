// ---- File: src/Core/WMS.Application/Features/WarehouseSetup/Queries/GetPalletTypesQuery.cs [NEW] ----
using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models; // For PagedResult

namespace WMS.Application.Features.WarehouseSetup.Queries;

// DTO for list view
public record PalletTypeDto(
    Guid Id,
    string Name,
    decimal TareWeight,
    decimal Length,
    decimal Width,
    decimal Height,
    bool IsActive);

// Query definition with pagination, sorting, filtering
public record GetPalletTypesQuery : IRequest<PagedResult<PalletTypeDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
    public string? SearchTerm { get; init; }
}

// Query Handler
public class GetPalletTypesQueryHandler(IPalletTypeRepository palletTypeRepository)
    : IRequestHandler<GetPalletTypesQuery, PagedResult<PalletTypeDto>>
{
    public async Task<PagedResult<PalletTypeDto>> Handle(GetPalletTypesQuery request, CancellationToken cancellationToken)
    {
        // Assuming GetPagedListAsync exists/will be added to the repository
        return await palletTypeRepository.GetPagedListAsync(request, cancellationToken);
    }
}