// ---- File: src/Core/WMS.Domain/Entities/Transaction/ItemTransferTransaction.cs ----

using WMS.Domain.Shared;

namespace WMS.Domain.Entities.Transaction;

public class ItemTransferTransaction : AuditableEntity<Guid>
{
    /// <summary>
    /// The specific inventory record the items were taken FROM.
    /// </summary>
    public Guid SourceInventoryId { get; private set; }

    /// <summary>
    /// The NEW pallet that was created to hold the transferred items.
    /// </summary>
    public Guid NewDestinationPalletId { get; private set; }

    /// <summary>
    /// The quantity of the material that was moved.
    /// </summary>
    public decimal QuantityTransferred { get; private set; }

    /// <summary>
    /// The weight of the material that was moved.
    /// </summary>
    public decimal WeightTransferred { get; private set; }

    public Guid UserId { get; private set; }
    public DateTime Timestamp { get; private set; }

    // Navigation Properties for reporting
    public MaterialInventory SourceInventory { get; private set; } = null!;
    public Pallet NewDestinationPallet { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private ItemTransferTransaction() : base(Guid.Empty) { }

    public static ItemTransferTransaction Create(Guid sourceInventoryId, Guid newDestinationPalletId, decimal quantity, decimal weight, Guid userId)
    {
        return new ItemTransferTransaction
        {
            Id = Guid.NewGuid(),
            SourceInventoryId = sourceInventoryId,
            NewDestinationPalletId = newDestinationPalletId,
            QuantityTransferred = quantity,
            WeightTransferred = weight,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        };
    }
}