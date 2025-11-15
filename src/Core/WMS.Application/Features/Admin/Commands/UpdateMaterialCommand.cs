using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Admin.Commands;

public record UpdateMaterialCommand(
    Guid Id,
    string Name,
    string Description,
    decimal CostPerUnit,
    Guid CategoryId,
    string RequiredTempZone,
    decimal BaseWeight,
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
    ) : IRequest;
public class UpdateMaterialCommandHandler(
    IMaterialRepository materialRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateMaterialCommand>
{
    public async Task Handle(UpdateMaterialCommand request, CancellationToken cancellationToken)
    {
        var material = await materialRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Material with ID {request.Id} not found.");

        var tempZone = Enum.Parse<TempZone>(request.RequiredTempZone, true);

        material.UpdateBasicInfo(
            request.Name,
            request.Description,
            request.CostPerUnit,
            request.CategoryId);

        material.UpdateLogistics(
            tempZone,
            request.BaseWeight,
            material.MaterialType);         

        material.UpdateHandlingRules(
            request.Perishable,
            request.ShelfLifeDays,
            request.IsHazardous);

        material.UpdateGs1Prefix(request.Gs1BarcodePrefix);

        material.UpdateDimensions(request.DimensionsLength, request.DimensionsWidth, request.DimensionsHeight, request.PackageTareWeightPerUom);
        material.UpdateStockLevels(request.MinStockLevel, request.MaxStockLevel);
        if (request.IsActive)
        {
            material.Activate();
        }
        else
        {
            material.Deactivate();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}