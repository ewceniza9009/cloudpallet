using System.Text.Json.Serialization;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities;

public class BillOfMaterial : AggregateRoot<Guid>
{
    [JsonInclude]
    public Guid OutputMaterialId { get; private set; }

    [JsonInclude]
    public decimal OutputQuantity { get; private set; }

    private readonly List<BillOfMaterialLine> _lines = new();
    public IReadOnlyCollection<BillOfMaterialLine> Lines => _lines.AsReadOnly();

    [JsonConstructor]
    private BillOfMaterial() : base(Guid.Empty) { }

    private BillOfMaterial(Guid id, Guid outputMaterialId, decimal outputQuantity) : base(id)
    {
        if (outputMaterialId == Guid.Empty) throw new ArgumentException("Output Material ID cannot be empty.", nameof(outputMaterialId));
        if (outputQuantity <= 0) throw new ArgumentException("Output quantity must be positive.", nameof(outputQuantity));

        OutputMaterialId = outputMaterialId;
        OutputQuantity = outputQuantity;
    }

    public static BillOfMaterial Create(Guid outputMaterialId, decimal outputQuantity)
    {
        return new BillOfMaterial(Guid.NewGuid(), outputMaterialId, outputQuantity);
    }

    public void AddLine(Guid inputMaterialId, decimal inputQuantity)
    {
        if (inputMaterialId == Guid.Empty) throw new ArgumentException("Input Material ID cannot be empty.", nameof(inputMaterialId));
        if (inputQuantity <= 0) throw new ArgumentException("Input quantity must be positive.", nameof(inputQuantity));
        if (_lines.Any(l => l.InputMaterialId == inputMaterialId)) throw new InvalidOperationException("This material is already an input in the BOM.");

        _lines.Add(new BillOfMaterialLine(this.Id, inputMaterialId, inputQuantity));
    }
}

public class BillOfMaterialLine : Entity<Guid>
{
    [JsonInclude]
    public Guid BillOfMaterialId { get; private set; }
    [JsonInclude]
    public Guid InputMaterialId { get; private set; }
    [JsonInclude]
    public decimal InputQuantity { get; private set; }

    [JsonConstructor]
    private BillOfMaterialLine() : base(Guid.Empty) { }

    internal BillOfMaterialLine(Guid billOfMaterialId, Guid inputMaterialId, decimal inputQuantity) : base(Guid.NewGuid())
    {
        BillOfMaterialId = billOfMaterialId;
        InputMaterialId = inputMaterialId;
        InputQuantity = inputQuantity;
    }
}