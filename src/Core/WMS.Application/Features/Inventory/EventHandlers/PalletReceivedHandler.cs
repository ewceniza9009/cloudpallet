// ---- File: src/Core/WMS.Application/Features/Inventory/EventHandlers/PalletReceivedHandler.cs ----

using MediatR;
using Microsoft.Extensions.Logging;
using WMS.Application.Abstractions.Integrations;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Abstractions.Security;
using WMS.Application.Features.Inventory.Commands;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Events;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.Inventory.EventHandlers;

public class PalletReceivedHandler(
    ISender sender,
    IMaterialInventoryRepository inventoryRepository,
    IReceivingTransactionRepository receivingRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IBarcodeGenerationService barcodeGenerationService,
    ILogger<PalletReceivedHandler> logger) // Added Logger
    : INotificationHandler<PalletReceivedEvent>
{
    public async Task Handle(PalletReceivedEvent notification, CancellationToken cancellationToken)
    {
        // Use a default user if none is available (e.g., system process)
        var currentUserId = currentUserService.UserId ?? Guid.Empty; // Use Guid.Empty or a specific system user ID
        logger.LogInformation("Handling PalletReceivedEvent for PalletId: {PalletId}", notification.PalletId);

        var receivingId = await receivingRepository.GetReceivingIdForPalletAsync(notification.PalletId, cancellationToken)
            ?? throw new InvalidOperationException($"Receiving record for Pallet ID {notification.PalletId} not found.");

        var receivingAggregate = await receivingRepository.GetByIdWithDetailsAsync(receivingId, cancellationToken)
            ?? throw new InvalidOperationException($"Receiving aggregate {receivingId} not found.");

        var pallet = receivingAggregate.Pallets.FirstOrDefault(p => p.Id == notification.PalletId)
            ?? throw new InvalidOperationException($"Pallet {notification.PalletId} not found in aggregate.");

        if (pallet.AccountId == Guid.Empty && receivingAggregate.AccountId.HasValue)
        {
            // Log if AccountId is missing on pallet but present on receiving - this shouldn't happen with current logic but good check
            logger.LogWarning("Pallet {PalletId} was missing AccountId, inheriting from Receiving {ReceivingId}", pallet.Id, receivingId);
        }
        else if (!receivingAggregate.AccountId.HasValue)
        {
            // Log critical error if Receiving itself is missing AccountId
            logger.LogError("Cannot create inventory for Pallet {PalletId} because parent Receiving {ReceivingId} has no AccountId.", pallet.Id, receivingId);
            throw new InvalidOperationException($"Receiving record {receivingId} is missing the required AccountId.");
        }

        foreach (var line in pallet.Lines)
        {
            // Ensure the line has been processed and has a barcode
            if (line.Status != Domain.Enums.PalletLineStatus.Processed || string.IsNullOrEmpty(line.Barcode))
            {
                logger.LogWarning("Skipping inventory creation for PalletLineId {PalletLineId} on PalletId {PalletId} because it is not processed or has no barcode.", line.Id, pallet.Id);
                continue; // Skip lines that haven't been fully processed
            }

            var itemLpn = line.Barcode; // Use the barcode already generated and assigned during processing

            var inventory = MaterialInventory.Create(
                line.MaterialId,
                notification.ReceivingLocationId, // Use the Staging location passed in the event
                pallet.Id,
                line.Id,
                line.Quantity,
                line.BatchNumber,
                Weight.Create(line.Weight, "KG"),
                line.ExpiryDate,
                pallet.AccountId, // Use AccountId from Pallet (inherited from Receiving)
                itemLpn
            );

            await inventoryRepository.AddAsync(inventory, cancellationToken);
            logger.LogInformation("Created MaterialInventory {InventoryId} for PalletLine {PalletLineId}", inventory.Id, line.Id);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Check if the pallet is NOT marked for cross-docking before triggering putaway
        if (!pallet.IsCrossDock)
        {
            var command = new PutawayGoodsCommand(pallet.Id, currentUserId);
            logger.LogInformation("Sending PutawayGoodsCommand for PalletId: {PalletId}", pallet.Id);
            await sender.Send(command, cancellationToken);
        }
        else
        {
            logger.LogInformation("Pallet {PalletId} is marked for Cross-Docking, skipping standard putaway command.", pallet.Id);
            // Optionally trigger a different command/event for cross-dock staging if needed
        }
    }
}