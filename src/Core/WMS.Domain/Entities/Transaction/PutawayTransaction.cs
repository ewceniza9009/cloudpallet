using WMS.Domain.Aggregates.Warehouse;   
using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities.Transaction;

public class PutawayTransaction : Entity<Guid>
{
    public Guid PalletId { get; private set; }
    public Guid LocationId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public TransactionStatus Status { get; private set; }

    public string? BatchNumber { get; private set; }
    public DateTime? ExpiryDate { get; private set; }

    public Location Location { get; set; } = null!;
    public Pallet Pallet { get; set; } = null!;   
    public User User { get; set; } = null!;   

    private PutawayTransaction() : base(Guid.Empty) { }

    public static PutawayTransaction Create(Guid palletId, Guid locationId, Guid userId, string? batchNumber, DateTime? expiryDate)
    {
        return new PutawayTransaction
        {
            Id = Guid.NewGuid(),
            PalletId = palletId,
            LocationId = locationId,
            UserId = userId,
            BatchNumber = batchNumber,
            ExpiryDate = expiryDate,
            Timestamp = DateTime.UtcNow,
            Status = TransactionStatus.Completed
        };
    }
}