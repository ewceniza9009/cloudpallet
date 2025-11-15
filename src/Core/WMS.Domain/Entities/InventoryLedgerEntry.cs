// ---- File: src/Core/WMS.Domain/Entities/InventoryLedgerEntry.cs ----
// [COMPLETE NEW FILE]

using WMS.Domain.Enums; // Required for LedgerTransactionType
using WMS.Domain.Shared; // Required for Entity<Guid>

namespace WMS.Domain.Entities;

/// <summary>
/// Denormalized record representing a single inventory movement for reporting.
/// This table is populated directly by command handlers upon successful inventory changes.
/// </summary>
public class InventoryLedgerEntry : Entity<Guid>
{
    // Core Info & Indexes
    public DateTime Timestamp { get; private set; }
    public Guid MaterialId { get; private set; }
    public Guid AccountId { get; private set; }
    public LedgerTransactionType TransactionType { get; private set; }
    public Guid TransactionId { get; private set; } // ID of the source transaction (PickId, ReceivingId, etc.)

    // Quantity & Weight Changes
    public decimal QuantityChange { get; private set; } // Positive for IN, Negative for OUT
    public decimal WeightChange { get; private set; }   // Positive for IN, Negative for OUT

    // Denormalized Data for Faster Reads
    public string MaterialName { get; private set; }
    public string AccountName { get; private set; }
    public string DocumentReference { get; private set; } // e.g., RECV-123, SHIP-456
    public string? UserName { get; private set; } // User performing action (Nullable)

    // Optional Contextual IDs (Indexed for Filtering)
    public Guid? LocationId { get; private set; } // Where it happened/ended up (Nullable)
    public Guid? PalletId { get; private set; }   // (Nullable)
    public Guid? SupplierId { get; private set; } // For Receiving (Nullable)
    public Guid? TruckId { get; private set; }    // For Receiving/Shipping (Nullable)
    public Guid? UserId { get; private set; }     // (Nullable)

    /// <summary>
    /// EF Core constructor.
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private InventoryLedgerEntry() : base(Guid.NewGuid()) // Generate ID on creation
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        // EF Core requires parameterless constructor.
        // Initialize strings to avoid warnings, though required fields are set by Create.
        MaterialName = string.Empty;
        AccountName = string.Empty;
        DocumentReference = string.Empty;
    }

    /// <summary>
    /// Static factory method used by Command Handlers to create new ledger entries.
    /// </summary>
    public static InventoryLedgerEntry Create(
        DateTime timestamp,
        Guid materialId,
        Guid accountId,
        LedgerTransactionType transactionType,
        Guid transactionId,
        decimal quantityChange,
        decimal weightChange,
        string materialName,
        string accountName,
        string documentReference,
        Guid? locationId,
        Guid? palletId,
        Guid? supplierId,
        Guid? truckId,
        Guid? userId,
        string? userName)
    {
        // Basic validation - ensure required fields are not empty Guids/strings
        if (materialId == Guid.Empty) throw new ArgumentException("MaterialId cannot be empty.", nameof(materialId));
        if (accountId == Guid.Empty) throw new ArgumentException("AccountId cannot be empty.", nameof(accountId));
        if (transactionId == Guid.Empty) throw new ArgumentException("TransactionId cannot be empty.", nameof(transactionId));
        if (string.IsNullOrWhiteSpace(materialName)) throw new ArgumentException("MaterialName cannot be empty.", nameof(materialName));
        if (string.IsNullOrWhiteSpace(accountName)) throw new ArgumentException("AccountName cannot be empty.", nameof(accountName));
        if (string.IsNullOrWhiteSpace(documentReference)) throw new ArgumentException("DocumentReference cannot be empty.", nameof(documentReference));

        // Create the instance
        var entry = new InventoryLedgerEntry
        {
            // Id is set in constructor
            Timestamp = timestamp,
            MaterialId = materialId,
            AccountId = accountId,
            TransactionType = transactionType,
            TransactionId = transactionId,
            QuantityChange = quantityChange,
            WeightChange = weightChange,
            MaterialName = materialName.Trim(), // Trim whitespace just in case
            AccountName = accountName.Trim(),   // Trim whitespace
            DocumentReference = documentReference.Trim(), // Trim whitespace
            LocationId = locationId,
            PalletId = palletId,
            SupplierId = supplierId,
            TruckId = truckId,
            UserId = userId,
            UserName = userName?.Trim() // Trim whitespace
        };

        return entry;
    }
}