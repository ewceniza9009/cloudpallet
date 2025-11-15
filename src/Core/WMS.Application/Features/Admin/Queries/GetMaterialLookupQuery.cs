using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Admin.Queries;

public record GetMaterialLookupQuery : IRequest<IEnumerable<MaterialDetailDto>>;

public class GetMaterialLookupQueryHandler(IMaterialRepository materialRepository)
    : IRequestHandler<GetMaterialLookupQuery, IEnumerable<MaterialDetailDto>>
{
    public async Task<IEnumerable<MaterialDetailDto>> Handle(GetMaterialLookupQuery request, CancellationToken cancellationToken)
    {
        var materials = await materialRepository.GetAllAsync(cancellationToken);

        return materials.Select(m => new MaterialDetailDto(
            m.Id,
            m.Name,
            m.Sku,
            m.Description,
            m.CategoryId,
            m.UomId,
            m.RequiredTempZone.ToString(),
            m.BaseWeight,
            m.CostPerUnit,
            m.MaterialType,
            m.Perishable,
            m.ShelfLifeDays,
            m.IsHazardous,
            m.Gs1BarcodePrefix,
            m.IsActive,
            m.DefaultBarcodeFormat,
            m.DimensionsLength,
            m.DimensionsWidth,
            m.DimensionsHeight,
            m.MinStockLevel,
            m.MaxStockLevel,
            m.PackageTareWeightPerUom
        ));
    }
}