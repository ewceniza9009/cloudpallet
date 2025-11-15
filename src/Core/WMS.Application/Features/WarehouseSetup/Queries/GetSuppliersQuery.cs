using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models;     

namespace WMS.Application.Features.WarehouseSetup.Queries;

public record SupplierDto(
    Guid Id,
    string Name,
    string? Phone,
    string? City,
    bool IsActive);

public record GetSuppliersQuery : IRequest<PagedResult<SupplierDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
    public string? SearchTerm { get; init; }
}

public class GetSuppliersQueryHandler(ISupplierRepository supplierRepository)
    : IRequestHandler<GetSuppliersQuery, PagedResult<SupplierDto>>
{
    public async Task<PagedResult<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        return await supplierRepository.GetPagedListAsync(request, cancellationToken);
    }
}