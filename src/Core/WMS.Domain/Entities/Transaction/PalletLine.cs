// ---- File: src/Core/WMS.Domain/Entities/Transaction/PalletLine.cs ----

using System.Text.Json.Serialization;
using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities.Transaction;

public class PalletLine : AuditableEntity<Guid> // MODIFIED: Changed from Entity<Guid>
{
    [JsonInclude]
    public Guid PalletId { get; private set; }
    [JsonInclude]
    public Guid MaterialId { get; private set; }
    [JsonInclude]
    public decimal Quantity { get; private set; }
    [JsonInclude]
    public decimal Weight { get; private set; }
    [JsonInclude]
    public string BatchNumber { get; private set; }
    [JsonInclude]
    public DateTime? ExpiryDate { get; private set; }
    [JsonInclude]
    public Guid AccountId { get; private set; }
    [JsonInclude]
    public DateTime DateOfManufacture { get; private set; }
    [JsonInclude]
    public PalletLineStatus Status { get; private set; }
    [JsonInclude]
    public string Barcode { get; private set; }

    public Material Material { get; private set; } = null!;
    public Pallet Pallet { get; private set; } = null!;

    [JsonConstructor]
    private PalletLine() : base(Guid.Empty)
    {
        BatchNumber = null!;
        Barcode = null!;
    }

    public static PalletLine Create(Guid palletId, Guid materialId, decimal quantity, decimal weight, string batchNumber, DateTime dateOfManufacture, DateTime? expiryDate, Guid accountId)
    {
        return new PalletLine(Guid.NewGuid(), palletId, materialId, quantity, weight, batchNumber, dateOfManufacture, expiryDate, accountId);
    }

    public void Update(decimal quantity, decimal weight, string batchNumber, DateTime dateOfManufacture, DateTime? expiryDate)
    {
        Quantity = quantity;
        Weight = weight;
        BatchNumber = batchNumber;
        DateOfManufacture = dateOfManufacture;
        ExpiryDate = expiryDate;
        Status = PalletLineStatus.Processed;
    }

    public void SetBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw new ArgumentException("Barcode cannot be empty.", nameof(barcode));
        }
        Barcode = barcode;
    }

    public void Reset()
    {
        Quantity = 0;
        Weight = 0;
        BatchNumber = "PENDING";
        ExpiryDate = null;
        Status = PalletLineStatus.Pending;
        Barcode = string.Empty;
    }

    private PalletLine(Guid id, Guid palletId, Guid materialId, decimal quantity, decimal weight, string batchNumber, DateTime dateOfManufacture, DateTime? expiryDate, Guid accountId) : base(id)
    {
        PalletId = palletId;
        MaterialId = materialId;
        Quantity = quantity;
        Weight = weight;
        BatchNumber = batchNumber;
        DateOfManufacture = dateOfManufacture;
        ExpiryDate = expiryDate;
        AccountId = accountId;
        Status = PalletLineStatus.Pending;
        Barcode = string.Empty;
    }

    internal void SetPalletId(Guid palletId)
    {
        if (PalletId != Guid.Empty)
        {
            throw new InvalidOperationException("Pallet ID cannot be changed once it is set.");
        }
        PalletId = palletId;
    }
}
