using System.Text.Json.Serialization;
using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Domain.Aggregates.Warehouse;

public class Dock : Entity<Guid>
{
    [JsonInclude]
    public Guid WarehouseId { get; private set; }
    [JsonInclude]
    public string Name { get; private set; }
    [JsonInclude]
    public DockType Type { get; private set; }
    [JsonInclude]
    public int EquipmentFlags { get; private set; }
    [JsonInclude]
    public decimal CapacityTruckLength { get; private set; }
    [JsonInclude]
    public bool IsActive { get; private set; }
    [JsonInclude]
    public Guid? CurrentAppointmentId { get; private set; }

    [JsonConstructor]
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