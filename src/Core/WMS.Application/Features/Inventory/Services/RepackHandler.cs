using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.Inventory.Commands;
using WMS.Domain.Constants;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.Inventory.Services;

public class RepackHandler(
    IMaterialInventoryRepository inventoryRepository,
    IBillOfMaterialRepository bomRepository,
    IMaterialRepository materialRepository,
    IVASTransactionRepository vasRepository,
    IUnitOfWork unitOfWork) : IVasServiceHandler
{
    public ServiceType ServiceType => ServiceType.Repack;

    public async Task HandleAsync(RecordVasCommand command, CancellationToken cancellationToken)
    {
        if (!command.SourceInventoryId.HasValue || !command.TargetMaterialId.HasValue || !command.QuantityToProcess.HasValue)
            throw new InvalidOperationException("Source Inventory, Target Material, and Quantity are required for Repacking.");

        var bom = await bomRepository.GetByOutputMaterialIdAsync(command.TargetMaterialId.Value, cancellationToken)
            ?? throw new InvalidOperationException($"No repackaging recipe (Bill of Materials) found for the target material ID {command.TargetMaterialId.Value}.");

        if (bom.Lines.Count != 1)
        {
            throw new NotSupportedException("The system currently only supports single-input repackaging.");
        }
        var bomLine = bom.Lines.Single();

        var sourceInventory = await inventoryRepository.GetByIdWithTrackingAsync(command.SourceInventoryId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("Source inventory not found.");

        var sourceMaterial = await materialRepository.GetByIdAsync(sourceInventory.MaterialId, cancellationToken)
            ?? throw new KeyNotFoundException("Source material not found.");

        if (sourceInventory.MaterialId != bomLine.InputMaterialId)
        {
            throw new InvalidOperationException("The selected source inventory material does not match the required input material in the repackaging recipe.");
        }

        if (sourceInventory.Quantity < command.QuantityToProcess.Value)
            throw new InvalidOperationException("Insufficient quantity in source inventory to perform repackaging.");

        decimal outputQuantity = (command.QuantityToProcess.Value / bomLine.InputQuantity) * bom.OutputQuantity;
        decimal inputWeight = command.QuantityToProcess.Value * sourceMaterial.BaseWeight;

        var targetMaterial = await materialRepository.GetByIdAsync(command.TargetMaterialId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("Target material not found.");
        decimal outputWeight = outputQuantity * targetMaterial.BaseWeight;

        var description = $"Repacked {command.QuantityToProcess.Value} of '{sourceMaterial.Name}' into {outputQuantity} of '{targetMaterial.Name}'";

        var vasTransaction = VASTransaction.Create(
            sourceInventory.AccountId,
            sourceInventory.PalletId,
            ServiceType.Repack,
            command.UserId,
            description);

        vasTransaction.AddInputLine(sourceInventory.MaterialId, command.QuantityToProcess.Value, inputWeight, sourceInventory.BatchNumber, sourceInventory.ExpiryDate);
        vasTransaction.AddOutputLine(command.TargetMaterialId.Value, outputQuantity, outputWeight, sourceInventory.BatchNumber, sourceInventory.ExpiryDate);

        // Add Labor Line for Billing
        if (command.DurationHours.HasValue && command.DurationHours.Value > 0)
        {
            vasTransaction.AddInputLine(null, command.DurationHours.Value, 0);
        }

        // Adjust source inventory using new method
        sourceInventory.AdjustInventory(-command.QuantityToProcess.Value, -inputWeight);

        // Create new inventory for the repacked item
        var newInventory = MaterialInventory.Create(
            command.TargetMaterialId.Value,
            sourceInventory.LocationId, 
            sourceInventory.PalletId,   
            Guid.NewGuid(), 
            outputQuantity,
            sourceInventory.BatchNumber, 
            Weight.Create(outputWeight, "KG"),
            sourceInventory.ExpiryDate, 
            sourceInventory.AccountId,
            $"{DomainConstants.BarcodePrefixes.Repack}{Guid.NewGuid().ToString().Substring(0, 8)}"); // Use Constant

        await inventoryRepository.AddAsync(newInventory, cancellationToken);
        await vasRepository.AddAsync(vasTransaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
