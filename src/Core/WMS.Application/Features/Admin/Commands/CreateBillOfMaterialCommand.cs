// ---- File: src/Core/WMS.Application/Features/Admin/Commands/CreateBillOfMaterialCommandHandler.cs ----

using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.Enums; // <-- ADDED

namespace WMS.Application.Features.Admin.Commands;

public record BomLineDto(Guid InputMaterialId, decimal InputQuantity);

public record CreateBillOfMaterialCommand(
    Guid OutputMaterialId,
    decimal OutputQuantity,
    List<BomLineDto> Lines) : IRequest<Guid>;

public class CreateBillOfMaterialCommandHandler(
    IBillOfMaterialRepository bomRepository,
    IMaterialRepository materialRepository, // <-- ADDED
    IUnitOfWork unitOfWork) : IRequestHandler<CreateBillOfMaterialCommand, Guid>
{
    public async Task<Guid> Handle(CreateBillOfMaterialCommand request, CancellationToken cancellationToken)
    {
        // --- START: NEW VALIDATION ---
        var material = await materialRepository.GetByIdAsync(request.OutputMaterialId, cancellationToken)
            ?? throw new KeyNotFoundException($"Output material with ID {request.OutputMaterialId} not found.");

        // Rule 1: Cannot add a BOM to a 'Normal' material
        if (material.MaterialType == MaterialType.Normal)
        {
            throw new InvalidOperationException($"Cannot create a recipe for a '{MaterialType.Normal}' material. Please set the material type to 'Kit' or 'Repack'.");
        }

        // Rule 2: A 'Repack' material can only have exactly one component line
        if (material.MaterialType == MaterialType.Repack && request.Lines.Count != 1)
        {
            throw new InvalidOperationException($"A '{MaterialType.Repack}' material must have exactly one component. To add multiple components, change the material type to 'Kit'.");
        }
        // --- END: NEW VALIDATION ---

        var existingBom = await bomRepository.GetByOutputMaterialIdAsync(request.OutputMaterialId, cancellationToken);
        if (existingBom != null)
        {
            // If BOM exists, update it instead of throwing error (more user-friendly)
            // For now, we delete the old and create new. A real app might update lines.
            bomRepository.Remove(existingBom);
        }

        var bom = BillOfMaterial.Create(request.OutputMaterialId, request.OutputQuantity);

        foreach (var line in request.Lines)
        {
            bom.AddLine(line.InputMaterialId, line.InputQuantity);
        }

        await bomRepository.AddAsync(bom, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return bom.Id;
    }
}