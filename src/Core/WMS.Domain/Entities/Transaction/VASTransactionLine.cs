using WMS.Domain.Entities;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities.Transaction;

public class VASTransactionLine : Entity<Guid>
{
    public Guid VASTransactionId { get; private set; }
    public Guid? MaterialId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal Weight { get; private set; }
    public bool IsInput { get; private set; }

    // Amendment tracking
    public decimal? OriginalQuantity { get; private set; }
    public decimal? OriginalWeight { get; private set; }
    public bool IsAmended { get; private set; }
    public DateTime? AmendedAt { get; private set; }

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
        IsAmended = false;
    }

    public void AmendQuantity(decimal newQuantity)
    {
        if (!IsAmended)
        {
            // Store original value on first amendment
            OriginalQuantity = Quantity;
        }
        Quantity = newQuantity;
        IsAmended = true;
        AmendedAt = DateTime.UtcNow;
    }

    public void AmendWeight(decimal newWeight)
    {
        if (!IsAmended)
        {
            // Store original value on first amendment
            OriginalWeight = Weight;
        }
        Weight = newWeight;
        IsAmended = true;
        AmendedAt = DateTime.UtcNow;
    }

    public void AmendQuantityAndWeight(decimal newQuantity, decimal newWeight)
    {
        if (!IsAmended)
        {
            // Store original values on first amendment
            OriginalQuantity = Quantity;
            OriginalWeight = Weight;
        }
        Quantity = newQuantity;
        Weight = newWeight;
        IsAmended = true;
        AmendedAt = DateTime.UtcNow;
    }
}