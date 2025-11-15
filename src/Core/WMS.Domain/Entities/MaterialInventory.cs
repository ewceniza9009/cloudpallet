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

    public void AdjustQuantity(decimal delta)
    {
        if (Quantity + delta < 0)
        {
            throw new InvalidOperationException("Inventory quantity cannot be negative.");
        }
        Quantity += delta;
    }

    public void AdjustForWeighedPick(decimal pickedQuantity, decimal pickedWeight)
    {
        if (Quantity - pickedQuantity < 0)
        {
            throw new InvalidOperationException("Picked quantity cannot be greater than available quantity.");
        }
        if (WeightActual.Value - pickedWeight < 0)
        {
            if (Math.Abs(WeightActual.Value - pickedWeight) > 5)       
            {
                throw new InvalidOperationException("Picked weight results in significant negative inventory weight.");
            }
        }

        Quantity -= pickedQuantity;
        WeightActual = Weight.Create(WeightActual.Value - pickedWeight, WeightActual.Unit);
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