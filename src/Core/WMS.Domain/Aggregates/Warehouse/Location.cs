using System.Text.Json.Serialization;
using WMS.Domain.Enums;
using WMS.Domain.Shared;
using WMS.Domain.ValueObjects;

namespace WMS.Domain.Aggregates.Warehouse;

public class Location : Entity<Guid>
{
    [JsonInclude]
    public Guid RoomId { get; private set; }
    [JsonInclude]
    public string Bay { get; private set; }
    [JsonInclude]
    public int Row { get; private set; }
    [JsonInclude]
    public int Column { get; private set; }
    [JsonInclude]
    public int Level { get; private set; }
    [JsonInclude]
    public Weight CapacityWeight { get; private set; }
    [JsonInclude]
    public decimal CapacityVolume { get; private set; }
    [JsonInclude]
    public LocationType ZoneType { get; private set; }
    [JsonInclude]
    public bool IsEmpty { get; private set; }
    [JsonInclude]
    public bool IsActive { get; private set; }
    [JsonInclude]
    public string Barcode { get; private set; }

    public Room Room { get; private set; } = null!;

    [JsonConstructor]
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