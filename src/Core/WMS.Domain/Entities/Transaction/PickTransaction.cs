using WMS.Domain.Enums;
using WMS.Domain.Shared;
using WMS.Domain.Entities;   

namespace WMS.Domain.Entities.Transaction;

public class PickTransaction : AuditableEntity<Guid>
{
    public Guid InventoryId { get; private set; }
    public Guid? PalletId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal PickWeight { get; private set; }
    public PickReason Reason { get; private set; }
    public string ScannedBarcode { get; private set; }
    public Guid AccountId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public PickStatus Status { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsExpedited { get; private set; } // <-- NEW FLAG
    public string? BatchNumber { get; private set; }
    public DateTime? ExpiryDate { get; private set; }

    public MaterialInventory MaterialInventory { get; private set; } = null!;
    public Account Account { get; private set; } = null!;
    public User User { get; private set; } = null!;  
    public ICollection<WithdrawalTransaction> WithdrawalTransactions { get; set; } = new List<WithdrawalTransaction>();

    private PickTransaction() : base(Guid.Empty)
    {
        ScannedBarcode = null!;
    }

    public static PickTransaction Create(Guid inventoryId, decimal quantity, Guid userId, Guid accountId, string? batchNumber, DateTime? expiryDate, bool isExpedited = false) // <-- ADD isExpedited
    {
        var pick = new PickTransaction(Guid.NewGuid(), inventoryId, quantity, userId, accountId, batchNumber, expiryDate);
        pick.IsExpedited = isExpedited; // <-- SET FLAG
        return pick;
    }

    private PickTransaction(Guid id, Guid inventoryId, decimal quantity, Guid userId, Guid accountId, string? batchNumber, DateTime? expiryDate) : base(id)
    {
        InventoryId = inventoryId;
        Quantity = quantity;
        UserId = userId;
        AccountId = accountId;
        BatchNumber = batchNumber;
        ExpiryDate = expiryDate;
        Reason = PickReason.Order;
        Status = PickStatus.Planned;
        Timestamp = DateTime.UtcNow;
        ScannedBarcode = string.Empty;
        PickWeight = 0;
        IsExpedited = false; // Default
    }

    public void ConfirmPick(PickStatus newStatus, decimal actualWeight)
    {
        Status = newStatus;
        PickWeight = actualWeight;
    }
}