using WMS.Domain.Shared;

namespace WMS.Domain.Aggregates.Cargo;

public class CargoManifest : AggregateRoot<Guid>
{
    public Guid DockAppointmentId { get; private set; }
    private readonly List<CargoManifestLine> _lines = new();
    public IReadOnlyCollection<CargoManifestLine> Lines => _lines.AsReadOnly();

    private CargoManifest(Guid id, Guid appointmentId) : base(id)
    {
        DockAppointmentId = appointmentId;
    }

    public static CargoManifest Create(Guid appointmentId)
    {
        return new CargoManifest(Guid.NewGuid(), appointmentId);
    }

    public void AddLine(Guid materialId, decimal quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        _lines.Add(new CargoManifestLine(Guid.NewGuid(), Id, materialId, quantity));
    }

#pragma warning disable CS8618
    private CargoManifest() { }    
#pragma warning restore CS8618
}

public class CargoManifestLine : Entity<Guid>
{
    public Guid CargoManifestId { get; private set; }
    public Guid MaterialId { get; private set; }
    public decimal Quantity { get; private set; }

    internal CargoManifestLine(Guid id, Guid manifestId, Guid materialId, decimal quantity) : base(id)
    {
        CargoManifestId = manifestId;
        MaterialId = materialId;
        Quantity = quantity;
    }

#pragma warning disable CS8618
    private CargoManifestLine() { }    
#pragma warning restore CS8618
}