using WMS.Domain.Enums;
using WMS.Domain.Shared;
using WMS.Domain.ValueObjects;

namespace WMS.Domain.Aggregates.Warehouse;

public class Location : Entity<Guid>
{
    public Guid RoomId { get; private set; }
    public string Bay { get; private set; }
    public int Row { get; private set; }
    public int Column { get; private set; }
    public int Level { get; private set; }
    public Weight CapacityWeight { get; private set; }
    public decimal CapacityVolume { get; private set; }
    public LocationType ZoneType { get; private set; }
    public bool IsEmpty { get; private set; }
    public bool IsActive { get; private set; }
    public string Barcode { get; private set; }

    public Room Room { get; private set; } = null!;

    private Location() : base(Guid.Empty)
    {
        Bay = null!;
        CapacityWeight = null!;
        Barcode = null!;
    }

    internal Location(Guid id, Guid roomId, string bay, int row, int column, int level, LocationType zoneType) : base(id)
    {
        RoomId = roomId;
        Bay = bay;
        Row = row;
        Column = column;
        Level = level;
        ZoneType = zoneType;
        IsActive = true;
        IsEmpty = true;
        CapacityWeight = Weight.Create(1000, "KG");   

        if (zoneType == LocationType.Storage)
        {
            Barcode = $"{bay}-{row}-{column}-{level}";
        }
        else
        {
            Barcode = bay;
        }
    }
    public void Update(LocationType zoneType, Weight capacityWeight, bool isActive)
    {
        ZoneType = zoneType;
        CapacityWeight = capacityWeight;
        IsActive = isActive;
    }
    public void MarkAsFilled() => IsEmpty = false;
    public void MarkAsEmpty() => IsEmpty = true;
}