using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Domain.Aggregates.Warehouse;

public class Dock : Entity<Guid>
{
    public Guid WarehouseId { get; private set; }
    public string Name { get; private set; }
    public DockType Type { get; private set; }
    public int EquipmentFlags { get; private set; }
    public decimal CapacityTruckLength { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? CurrentAppointmentId { get; private set; }

    private Dock() : base(Guid.Empty)
    {
        Name = null!;
    }

    internal Dock(Guid id, Guid warehouseId, string name, DockType type) : base(id)
    {
        WarehouseId = warehouseId;
        Name = name;
        Type = type;
        IsActive = true;
    }

    public void Occupy(Guid appointmentId)
    {
        if (CurrentAppointmentId.HasValue) throw new InvalidOperationException("Dock is already occupied.");
        CurrentAppointmentId = appointmentId;
    }

    public void Vacate()
    {
        CurrentAppointmentId = null;
    }

    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Dock name cannot be empty.", nameof(name));
        }
        Name = name;
        // Add DockType to this method later if needed
    }
}