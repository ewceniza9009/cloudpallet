using WMS.Domain.Aggregates.Warehouse;
using WMS.Domain.Entities;   
using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities.Transaction;

public class TransferTransaction : Entity<Guid>
{
    public Guid PalletId { get; private set; }
    public Guid FromLocationId { get; private set; }
    public Guid ToLocationId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public TransactionStatus Status { get; private set; }

    public Location FromLocation { get; set; } = null!;
    public Location ToLocation { get; set; } = null!;
    public Pallet Pallet { get; set; } = null!;  
    public User User { get; set; } = null!;  

    private TransferTransaction() : base(Guid.Empty) { }

    public static TransferTransaction Create(Guid palletId, Guid fromLocationId, Guid toLocationId, Guid userId)
    {
        if (fromLocationId == toLocationId)
        {
            throw new InvalidOperationException("Source and destination locations cannot be the same for a transfer.");
        }

        return new TransferTransaction
        {
            Id = Guid.NewGuid(),
            PalletId = palletId,
            FromLocationId = fromLocationId,
            ToLocationId = toLocationId,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            Status = TransactionStatus.Completed
        };
    }

    private TransferTransaction(Guid id, Guid palletId, Guid fromLocationId, Guid toLocationId, Guid userId) : base(id)
    {
        PalletId = palletId;
        FromLocationId = fromLocationId;
        ToLocationId = toLocationId;
        UserId = userId;
        Timestamp = DateTime.UtcNow;
        Status = TransactionStatus.Planned;
    }
}