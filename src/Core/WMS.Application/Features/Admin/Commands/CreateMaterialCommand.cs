using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Admin.Commands;

public record CreateMaterialCommand(
    string Name,
    string Sku,
    string Description,
    Guid CategoryId,
    Guid UomId,
    string RequiredTempZone,
    decimal BaseWeight,
    decimal CostPerUnit,
    MaterialType MaterialType,
    bool Perishable = false,
    int ShelfLifeDays = 0,
    bool IsHazardous = false,
    string? Gs1BarcodePrefix = null,
    BarcodeFormat DefaultBarcodeFormat = BarcodeFormat.GS1_128,
    decimal DimensionsLength = 0,
    decimal DimensionsWidth = 0,
    decimal DimensionsHeight = 0,
    int MinStockLevel = 0,
    int MaxStockLevel = 0,
    decimal PackageTareWeightPerUom = 0
    ) : IRequest<Guid>;
public class CreateMaterialCommandHandler(
    IMaterialRepository materialRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateMaterialCommand, Guid>
{
    public async Task<Guid> Handle(CreateMaterialCommand request, CancellationToken cancellationToken)
    {
        var tempZone = Enum.Parse<TempZone>(request.RequiredTempZone, true);

        var material = Material.Create(
            request.Name,
            request.Sku,
            request.Description,
            request.CategoryId,
            request.UomId,
            tempZone,
            request.BaseWeight,
            request.CostPerUnit,
            request.MaterialType);

        material.UpdateHandlingRules(request.Perishable, request.ShelfLifeDays, request.IsHazardous);
        material.UpdateGs1Prefix(request.Gs1BarcodePrefix ?? string.Empty);
        material.UpdateDimensions(request.DimensionsLength, request.DimensionsWidth, request.DimensionsHeight, request.PackageTareWeightPerUom);
        material.UpdateStockLevels(request.MinStockLevel, request.MaxStockLevel);
        await materialRepository.AddAsync(material, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return material.Id;
    }
}