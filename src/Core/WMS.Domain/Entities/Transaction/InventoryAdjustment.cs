// ---- File: src/Core/WMS.Domain/Entities/Transaction/InventoryAdjustment.cs ----

using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities.Transaction;

/// <summary>
/// Represents an auditable record of a manual inventory adjustment
/// (e.g., from a cycle count, damage, or expiry).
/// </summary>
public class InventoryAdjustment : AuditableEntity<Guid>
{
    /// <summary>
    /// The specific inventory record that was adjusted.
    /// </summary>
    public Guid InventoryId { get; private set; }

    /// <summary>
    /// The change in quantity (can be positive or negative).
    /// </summary>
    public decimal DeltaQuantity { get; private set; }

    /// <summary>
    /// The reason for the adjustment (e.g., Count, Damage, Expiry).
    /// </summary>
    public AdjustmentReason Reason { get; private set; }

    /// <summary>
    /// The account that owns this inventory.
    /// </summary>
    public Guid AccountId { get; private set; }

    /// <summary>
    /// The user who performed the adjustment.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The time the adjustment was recorded.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    // --- Navigation Properties ---
    public MaterialInventory Inventory { get; private set; } = null!;
    public Account Account { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private InventoryAdjustment() : base(Guid.Empty) { }

    public static InventoryAdjustment Create(
        Guid inventoryId,
        decimal deltaQuantity,
        AdjustmentReason reason,
        Guid accountId,
        Guid userId)
    {
        if (deltaQuantity == 0)
        {
            throw new ArgumentException("Delta quantity cannot be zero for an adjustment.", nameof(deltaQuantity));
        }

        return new InventoryAdjustment
        {
            Id = Guid.NewGuid(),
            InventoryId = inventoryId,
            DeltaQuantity = deltaQuantity,
            Reason = reason,
            AccountId = accountId,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        };
    }
}