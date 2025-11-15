// ---- File: src/Core/WMS.Application/Features/Inventory/Commands/StartQuarantineCommand.cs ----

using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums; // Required for InventoryStatus

namespace WMS.Application.Features.Inventory.Commands;

// Command definition
public record StartQuarantineCommand(
    Guid InventoryId, // The specific MaterialInventory ID to quarantine
    string Reason     // Reason for quarantine (optional, for logging/audit)
    ) : IRequest;

// Command handler
public class StartQuarantineCommandHandler(
    IMaterialInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<StartQuarantineCommand>
{
    public async Task Handle(StartQuarantineCommand request, CancellationToken cancellationToken)
    {
        // Retrieve the inventory item with tracking enabled
        var inventory = await inventoryRepository.GetByIdWithTrackingAsync(request.InventoryId, cancellationToken)
            ?? throw new KeyNotFoundException($"Inventory item with ID {request.InventoryId} not found.");

        // Check if already quarantined to avoid redundant operations
        if (inventory.Status == InventoryStatus.Quarantined)
        {
            // Optionally, throw an exception or just return if no action needed
            // throw new InvalidOperationException("Inventory item is already quarantined.");
            return;
        }

        // Call the domain method to update the status and start date
        inventory.StartQuarantine(request.Reason);

        // Save the changes to the database
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}