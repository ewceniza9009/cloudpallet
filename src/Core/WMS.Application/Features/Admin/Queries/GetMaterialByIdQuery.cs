using MediatR;
using WMS.Application.Abstractions.Persistence;
namespace WMS.Application.Features.Admin.Queries;

public record GetMaterialByIdQuery(Guid Id) : IRequest<MaterialDetailDto?>;

public class GetMaterialByIdQueryHandler(IMaterialRepository materialRepository)
    : IRequestHandler<GetMaterialByIdQuery, MaterialDetailDto?>
{
    public async Task<MaterialDetailDto?> Handle(GetMaterialByIdQuery request, CancellationToken cancellationToken)
    {
        var material = await materialRepository.GetByIdAsync(request.Id, cancellationToken);

        if (material is null)
        {
            return null;
        }

        return new MaterialDetailDto(
            material.Id,
            material.Name,
            material.Sku,
            material.Description,
            material.CategoryId,
            material.UomId,
            material.RequiredTempZone.ToString(),
            material.BaseWeight,
            material.CostPerUnit,
            material.MaterialType,
            material.Perishable,
            material.ShelfLifeDays,
            material.IsHazardous,
            material.Gs1BarcodePrefix,
            material.IsActive,
            material.DefaultBarcodeFormat,
            material.DimensionsLength,
            material.DimensionsWidth,
            material.DimensionsHeight,
            material.MinStockLevel,
            material.MaxStockLevel,
            material.PackageTareWeightPerUom
        );
    }
}