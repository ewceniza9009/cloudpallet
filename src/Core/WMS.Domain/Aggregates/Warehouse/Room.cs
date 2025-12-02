using System.Text.Json.Serialization;
using WMS.Domain.Enums;
using WMS.Domain.Shared;
using WMS.Domain.ValueObjects;

namespace WMS.Domain.Aggregates.Warehouse;

public class Room : Entity<Guid>
{
    [JsonInclude]
    public Guid WarehouseId { get; private set; }
    [JsonInclude]
    public string Name { get; private set; }
    [JsonInclude]
    public TemperatureRange TemperatureRange { get; private set; }
    [JsonInclude]
    public ServiceType ServiceType { get; private set; }
    [JsonInclude]
    public int CapacityPallets { get; private set; }
    [JsonInclude]
    public bool IsActive { get; private set; }

    private readonly List<Location> _locations = new();
    public IReadOnlyCollection<Location> Locations => _locations.AsReadOnly();

    [JsonConstructor]
    private Room() : base(Guid.Empty)
    {
        Name = null!;
        TemperatureRange = null!;
    }

    internal Room(Guid id, Guid warehouseId, string name, TemperatureRange temperatureRange, ServiceType serviceType) : base(id)
    {
        WarehouseId = warehouseId;
        Name = name;
        TemperatureRange = temperatureRange;
        ServiceType = serviceType;
        IsActive = true;
    }

    public static Room Create(Guid warehouseId, string name, decimal minTemp, decimal maxTemp, ServiceType serviceType)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Room name cannot be empty.", nameof(name));

        var tempRange = TemperatureRange.Create(minTemp, maxTemp);
        return new Room(Guid.NewGuid(), warehouseId, name, tempRange, serviceType);
    }
    public void AddLocationsInBay(
        string bay,
        int startRow, int endRow,
        int startCol, int endCol,
        int startLevel, int endLevel,
        LocationType zoneType)
    {
        if (string.IsNullOrWhiteSpace(bay))
            throw new ArgumentException("Bay name must be provided.", nameof(bay));
        if (startRow <= 0 || startCol <= 0 || startLevel <= 0 || endRow < startRow || endCol < startCol || endLevel < startLevel)
            throw new ArgumentException("Location ranges are invalid.");

        if (zoneType != LocationType.Storage)
            throw new InvalidOperationException("This method is for generating 'Storage' (rack) locations only. Use AddSimpleLocation for other types.");

        for (int r = startRow; r <= endRow; r++)
        {
            for (int c = startCol; c <= endCol; c++)
            {
                for (int l = startLevel; l <= endLevel; l++)
                {
                    var barcode = $"{bay}-{r}-{c}-{l}";
                    if (_locations.Any(loc => loc.Barcode.Equals(barcode, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    var newLocation = new Location(Guid.NewGuid(), this.Id, bay, r, c, l, zoneType);
                    _locations.Add(newLocation);
                }
            }
        }
    }

    public Location AddSimpleLocation(string name, LocationType zoneType)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Location name must be provided.", nameof(name));

        if (zoneType == LocationType.Storage)
            throw new InvalidOperationException("Cannot create a 'Storage' (rack) location with this method. Use AddLocationsInBay.");

        if (_locations.Any(l => l.Barcode.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"A location with the name/barcode '{name}' already exists in this room.");
        }

        var newLocation = new Location(Guid.NewGuid(), this.Id, name, 1, 1, 1, zoneType);
        _locations.Add(newLocation);
        return newLocation;
    }
}