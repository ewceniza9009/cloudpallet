using WMS.Domain.Aggregates.Warehouse;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums;
using WMS.Domain.Shared;
using WMS.Domain.ValueObjects;

namespace WMS.Domain.Entities;

public class MaterialInventory : Entity<Guid>
{
    public Guid MaterialId { get; private set; }
    public Guid LocationId { get; private set; }
    public Guid PalletId { get; private set; }
    public Guid PalletLineId { get; private set; }
    public decimal Quantity { get; private set; }
    public string BatchNumber { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public Weight WeightActual { get; private set; }
    public string Barcode { get; private set; }
    public Guid AccountId { get; private set; }
    public InventoryStatus Status { get; private set; }
    public ComplianceLabelType ComplianceLabelStatus { get; private set; } // <-- NEW PROPERTY
    public DateTime? QuarantineStartDate { get; private set; } // <-- NEW PROPERTY
    public DateTime? QuarantineEndDate { get; private set; } // <-- NEW PROPERTY
    public byte[] RowVersion { get; set; } // Optimistic Concurrency Token
    public Material Material { get; private set; } = null!;
    public Location Location { get; private set; } = null!;

    public Pallet Pallet { get; private set; } = null!;

    private MaterialInventory() : base(Guid.Empty)
    {
        BatchNumber = null!;
        WeightActual = null!;
        Barcode = null!;
    }

    public static MaterialInventory Create(Guid materialId, Guid locationId, Guid palletId, Guid palletLineId, decimal quantity, string batchNumber, Weight weight, DateTime? expiryDate, Guid accountId, string barcode)
    {
        var inventory = new MaterialInventory(Guid.NewGuid(), materialId, locationId, palletId, palletLineId, quantity, batchNumber, weight, expiryDate, accountId, barcode);
        inventory.ComplianceLabelStatus = ComplianceLabelType.None; // Default value
        return inventory;
    }

    private MaterialInventory(Guid id, Guid materialId, Guid locationId, Guid palletId, Guid palletLineId, decimal quantity, string batchNumber, Weight weight, DateTime? expiryDate, Guid accountId, string barcode) : base(id)
    {
        MaterialId = materialId;
        LocationId = locationId;
        PalletId = palletId;
        PalletLineId = palletLineId;
        Quantity = quantity;
        BatchNumber = batchNumber;
        WeightActual = weight;
        ExpiryDate = expiryDate;
        AccountId = accountId;
        Barcode = barcode;
        Status = InventoryStatus.Available;
        ComplianceLabelStatus = ComplianceLabelType.None; // Default value
    }

    public void UpdateDetails(decimal quantity, Weight weight, string batchNumber, DateTime? expiryDate)
    {
        Quantity = quantity;
        WeightActual = weight;
        BatchNumber = batchNumber;
        ExpiryDate = expiryDate;
    }

    public void AdjustInventory(decimal quantityDelta, decimal weightDelta)
    {
        if (Quantity + quantityDelta < 0)
        {
            throw new InvalidOperationException($"Insufficient inventory quantity. Available: {Quantity}, Requested Delta: {quantityDelta}");
        }

        // Allow some tolerance for weight calculation rounding errors, but generally weight shouldn't be negative
        if (WeightActual.Value + weightDelta < -0.01m) 
        {
             // If we are zeroing out quantity, we should zero out weight too, so strict check might be annoying if not exact.
             // But generally, we shouldn't have negative weight.
             if (Math.Abs(WeightActual.Value + weightDelta) > 0.05m) // 50g tolerance
             {
                 throw new InvalidOperationException($"Inventory weight cannot be negative. Available: {WeightActual.Value}, Requested Delta: {weightDelta}");
             }
             // Auto-correct small negative epsilon to 0
             weightDelta = -WeightActual.Value;
        }

        Quantity += quantityDelta;
        WeightActual = Weight.Create(Math.Max(0, WeightActual.Value + weightDelta), WeightActual.Unit);
    }

    [Obsolete("Use AdjustInventory(quantityDelta, weightDelta) instead.")]
    public void AdjustQuantity(decimal delta)
    {
        AdjustInventory(delta, 0);
    }

    [Obsolete("Use AdjustInventory(-pickedQuantity, -pickedWeight) instead.")]
    public void AdjustForWeighedPick(decimal pickedQuantity, decimal pickedWeight)
    {
        AdjustInventory(-pickedQuantity, -pickedWeight);
    }

    public void MoveToLocation(Guid newLocationId)
    {
        if (newLocationId == Guid.Empty) throw new ArgumentException("New location ID cannot be empty.", nameof(newLocationId));
        LocationId = newLocationId;
    }

    // --- NEW METHODS ---
    public void UpdateComplianceLabelStatus(ComplianceLabelType labelType)
    {
        ComplianceLabelStatus = labelType;
        // Potentially change InventoryStatus if needed, e.g., from AwaitingLabeling back to Available
        if (Status == InventoryStatus.AwaitingLabeling)
        {
            Status = InventoryStatus.Available;
        }
    }

    public void StartQuarantine(string reason) // Reason might be logged elsewhere (AuditTrail)
    {
        if (Status == InventoryStatus.Quarantined) return;
        Status = InventoryStatus.Quarantined;
        QuarantineStartDate = DateTime.UtcNow;
        QuarantineEndDate = null;
    }

    public void ReleaseFromQuarantine()
    {
        if (Status != InventoryStatus.Quarantined) return;
        Status = InventoryStatus.Available;
        QuarantineEndDate = DateTime.UtcNow;
    }
}