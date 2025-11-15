using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities.Transaction;           

public class VASTransaction : AggregateRoot<Guid>
{
    public Guid AccountId { get; private set; }
    public Guid? PalletId { get; private set; }
    public ServiceType ServiceType { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string Description { get; private set; }
    public TransactionStatus Status { get; private set; }

    public User User { get; private set; } = null!;
    public Account Account { get; private set; } = null!;

    // --- START: MODIFICATION ---
    // 1. Create ONE master list for all lines
    private readonly List<VASTransactionLine> _lines = new();

    // 2. Change public properties to be filtered views of the master list
    public IReadOnlyCollection<VASTransactionLine> InputLines => _lines.Where(l => l.IsInput).ToList().AsReadOnly();
    public IReadOnlyCollection<VASTransactionLine> OutputLines => _lines.Where(l => !l.IsInput).ToList().AsReadOnly();
    // --- END: MODIFICATION ---

    private VASTransaction() : base(Guid.Empty)
    {
        Description = null!;
    }

    public static VASTransaction Create(Guid accountId, Guid? palletId, ServiceType serviceType, Guid userId, string description)
    {
        return new VASTransaction(Guid.NewGuid(), accountId, palletId, serviceType, userId, description);
    }

    private VASTransaction(Guid id, Guid accountId, Guid? palletId, ServiceType serviceType, Guid userId, string description) : base(id)
    {
        AccountId = accountId;
        PalletId = palletId;
        ServiceType = serviceType;
        UserId = userId;
        Description = description;
        Timestamp = DateTime.UtcNow;
        Status = TransactionStatus.Planned;
    }

    public void SetAccount(Guid accountId)
    {
        if (AccountId == Guid.Empty)
        {
            AccountId = accountId;
        }
    }

    public void SetPallet(Guid palletId)
    {
        if (!PalletId.HasValue)
        {
            PalletId = palletId;
        }
    }

    // --- START: MODIFICATION ---
    // 3. Update Add methods to add to the SINGLE _lines list
    public void AddInputLine(Guid? materialId, decimal quantity, decimal weight)
    {
        var line = VASTransactionLine.Create(this.Id, materialId, quantity, weight, true);
        _lines.Add(line);
    }

    public void AddOutputLine(Guid? materialId, decimal quantity, decimal weight)
    {
        var line = VASTransactionLine.Create(this.Id, materialId, quantity, weight, false);
        _lines.Add(line);
    }
    // --- END: MODIFICATION ---

    public void Complete()
    {
        if (Status == TransactionStatus.Completed) return;
        Status = TransactionStatus.Completed;
    }
}