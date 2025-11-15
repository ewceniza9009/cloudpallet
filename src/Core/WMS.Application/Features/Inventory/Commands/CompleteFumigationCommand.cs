// ---- File: src/Core/WMS.Application/Features/Inventory/Commands/CompleteFumigationCommand.cs ----

using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities; // Needed for VASTransaction
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Inventory.Commands;

// Command definition
public record CompleteFumigationCommand(
    Guid InventoryId,    // The specific MaterialInventory ID to release
    decimal DurationHours // Optional, if billing per hour or needed for description
    ) : IRequest
{
    public Guid UserId { get; init; } // Set automatically by controller/context
}

// Command handler
public class CompleteFumigationCommandHandler(
    IMaterialInventoryRepository inventoryRepository,
    IVASTransactionRepository vasRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CompleteFumigationCommand>
{
    public async Task Handle(CompleteFumigationCommand request, CancellationToken cancellationToken)
    {
        // Retrieve the inventory item with tracking
        var inventory = await inventoryRepository.GetByIdWithTrackingAsync(request.InventoryId, cancellationToken)
            ?? throw new KeyNotFoundException($"Inventory item with ID {request.InventoryId} not found.");

        // Ensure the item is actually quarantined
        if (inventory.Status != InventoryStatus.Quarantined)
        {
            throw new InvalidOperationException("Inventory item is not currently quarantined.");
        }

        // Call domain method to update status and end date
        inventory.ReleaseFromQuarantine();

        // --- Create VAS Transaction for Billing ---
        var description = $"Completed Fumigation/Quarantine for item LPN {inventory.Barcode}.";
        // Append duration if provided and relevant for description/billing detail
        if (request.DurationHours > 0)
        {
            description += $" Duration: {request.DurationHours:F1} hrs.";
        }

        var vasTransaction = VASTransaction.Create(
            inventory.AccountId,
            inventory.PalletId, // Associate with the pallet
            ServiceType.Fumigation, // Use the specific service type
            request.UserId,
            description);

        // Add lines representing the item processed (input/output quantity are the same)
        // Billing rate (per Cycle or per Hour) will determine how quantity is used.
        // If billing per cycle, quantity could be 1. If per hour, use DurationHours.
        // Let's assume billing per cycle for this VAS line:
        vasTransaction.AddInputLine(inventory.MaterialId, 1, 0); // Quantity = 1 cycle, Weight irrelevant
        vasTransaction.AddOutputLine(inventory.MaterialId, 1, 0); // Quantity = 1 cycle
        // If billing per hour, it might look like:
        // vasTransaction.AddInputLine(inventory.MaterialId, request.DurationHours, 0);

        vasTransaction.Complete(); // Mark the VAS transaction as completed

        // Add the VAS transaction to the repository
        await vasRepository.AddAsync(vasTransaction, cancellationToken);

        // Save both the inventory status change and the new VAS transaction
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}