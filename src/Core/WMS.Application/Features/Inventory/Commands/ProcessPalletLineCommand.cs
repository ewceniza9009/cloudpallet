// ---- File: src/Core/WMS.Application/Features/Inventory/Commands/ProcessPalletLineCommand.cs ----

using MediatR;
using WMS.Application.Abstractions.Integrations;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Events;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.Inventory.Commands;

public record ProcessPalletLineCommand(
  Guid ReceivingId,
  Guid PalletId,
  Guid PalletLineId,
  Guid MaterialId,
  decimal Quantity,
  string BatchNumber,
  DateTime DateOfManufacture,
  DateTime? ExpiryDate,
  decimal GrossWeight,
  Guid UserId) : IRequest<string>;

public class ProcessPalletLineCommandHandler(
  IReceivingTransactionRepository receivingRepository,
  IBarcodeGenerationService barcodeGenerationService,
  IUnitOfWork unitOfWork,
  IPublisher publisher)
  : IRequestHandler<ProcessPalletLineCommand, string>
{
    public async Task<string> Handle(ProcessPalletLineCommand request, CancellationToken cancellationToken)
    {
        var receiving = await receivingRepository.GetByIdWithDetailsAsync(request.ReceivingId, cancellationToken)
          ?? throw new KeyNotFoundException($"Receiving session with ID {request.ReceivingId} not found.");

        var pallet = receiving.Pallets.FirstOrDefault(p => p.Id == request.PalletId)
          ?? throw new KeyNotFoundException($"Pallet with ID {request.PalletId} not found in the receiving session.");

        var netWeight = request.GrossWeight - pallet.TareWeight;
        if (netWeight <= 0)
        {
            throw new InvalidOperationException("Net weight must be positive. Check gross and tare weights.");
        }

        var palletLine = pallet.Lines
          .FirstOrDefault(x => x.Id == request.PalletLineId);

        if (palletLine is null)
        {
            palletLine = pallet.Lines.FirstOrDefault(x => x.MaterialId == request.MaterialId && x.PalletId == request.PalletId);

            if (palletLine is null)
            {
                throw new KeyNotFoundException($"Pallet Line for Material ID {request.MaterialId} not found on Pallet ID {request.PalletId}. This command is for updating an existing line.");
            }
        }

        // 1. Update line details first (sets status to Processed)
        palletLine.Update(request.Quantity, netWeight, request.BatchNumber, request.DateOfManufacture, request.ExpiryDate);

        string itemLpn;

        // 2. FIX: ONLY generate and set the barcode if it's currently empty/unassigned
        if (string.IsNullOrWhiteSpace(palletLine.Barcode))
        {
            itemLpn = barcodeGenerationService.GenerateItemBarcode(palletLine.MaterialId);
            palletLine.SetBarcode(itemLpn);
        }
        else
        {
            itemLpn = palletLine.Barcode; // Use existing barcode if already set
        }

        await receivingRepository.UpdatePalletLineAsync(palletLine, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Publish event to ensure MaterialInventory is updated/created
        var materialReceivedEvent = new MaterialReceivedEvent(
      receiving.Id,
      pallet.Id,
      palletLine.Id,
      palletLine.MaterialId,
      receiving.AccountId!.Value);
        await publisher.Publish(materialReceivedEvent, cancellationToken);

        return itemLpn;
    }
}