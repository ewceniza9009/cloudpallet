// ---- File: src/Core/WMS.Application/Features/Inventory/Commands/RecordVasCommand.cs ----

using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums;
using WMS.Domain.ValueObjects;
using WMS.Application.Features.Inventory.Services;

namespace WMS.Application.Features.Inventory.Commands;

// Enum to specify whether the labeling applies to a whole pallet or a specific item
public enum TargetType { Pallet, InventoryItem }

public record RecordVasCommand(
    Guid? PalletId,
    ServiceType ServiceType,
    Guid? SourceInventoryId,
    Guid? TargetMaterialId,
    decimal? QuantityToProcess,
    // --- Fields specifically for Labeling ---
    Guid? TargetId,
    TargetType? TargetType,
    ComplianceLabelType? LabelType,
    decimal? QuantityLabeled,
    // --- Fields specifically for Fumigation ---
    Guid? InventoryId, // Target for Fumigation
    decimal? DurationHours,
    // --- Fields specifically for Cycle Count ---
    List<CountItemDto>? CountedItems
    ) : IRequest
{
    public Guid UserId { get; init; }
}

public class RecordVasCommandHandler(
    IEnumerable<IVasServiceHandler> handlers)
    : IRequestHandler<RecordVasCommand>
{
    public async Task Handle(RecordVasCommand request, CancellationToken cancellationToken)
    {
        var handler = handlers.FirstOrDefault(h => h.ServiceType == request.ServiceType);

        if (handler == null)
        {
            if (request.ServiceType == ServiceType.Split)
            {
                 throw new NotSupportedException("ServiceType.Split is handled by the TransferItems command, not RecordVas.");
            }
            throw new NotSupportedException($"VAS type '{request.ServiceType}' is not supported for direct recording.");
        }

        await handler.HandleAsync(request, cancellationToken);
    }
}