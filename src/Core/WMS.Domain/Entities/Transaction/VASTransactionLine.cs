using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities.Transaction;

public class VASTransactionLine : Entity<Guid>
{
    public Guid VASTransactionId { get; private set; }
    public Guid? MaterialId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal Weight { get; private set; }
    public bool IsInput { get; private set; }

    public Material? Material { get; private set; }
    public VASTransaction VASTransaction { get; private set; } = null!;

    private VASTransactionLine() : base(Guid.Empty) { }

    public static VASTransactionLine Create(Guid vasTransactionId, Guid? materialId, decimal quantity, decimal weight, bool isInput)
    {
        return new VASTransactionLine(Guid.NewGuid(), vasTransactionId, materialId, quantity, weight, isInput);
    }

    private VASTransactionLine(Guid id, Guid vasTransactionId, Guid? materialId, decimal quantity, decimal weight, bool isInput) : base(id)
    {
        VASTransactionId = vasTransactionId;
        MaterialId = materialId;
        Quantity = quantity;
        Weight = weight;
        IsInput = isInput;
    }
}