using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Common.Models;     
using WMS.Domain.Enums;

public record MaterialDetailDto(
    Guid Id,
    string Name,
    string Sku,
    string Description,
    Guid CategoryId,
    Guid UomId,
    string RequiredTempZone,
    decimal BaseWeight,
    decimal CostPerUnit,
    MaterialType MaterialType,
    bool Perishable,
    int ShelfLifeDays,
    bool IsHazardous,
    string Gs1BarcodePrefix,
    bool IsActive,
    BarcodeFormat DefaultBarcodeFormat,
    decimal DimensionsLength,
    decimal DimensionsWidth,
    decimal DimensionsHeight,
    int MinStockLevel,
    int MaxStockLevel,
    decimal PackageTareWeightPerUom
    );

public record GetMaterialsQuery : IRequest<PagedResult<MaterialDetailDto>>     
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
    public string? SearchTerm { get; init; }
}
public class GetMaterialsQueryHandler(IMaterialRepository materialRepository)
    : IRequestHandler<GetMaterialsQuery, PagedResult<MaterialDetailDto>>     
{
    public async Task<PagedResult<MaterialDetailDto>> Handle(GetMaterialsQuery request, CancellationToken cancellationToken)
    {
        return await materialRepository.GetPagedListAsync(request, cancellationToken);
    }
}
