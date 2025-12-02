using System.Text.Json.Serialization;
using WMS.Domain.Enums;
using WMS.Domain.Shared;
using WMS.Domain.ValueObjects;

namespace WMS.Domain.Aggregates.Warehouse;

public class Warehouse : AggregateRoot<Guid>
{
    [JsonInclude]
    public Guid CompanyId { get; private set; }
    [JsonInclude]
    public string Name { get; private set; }
    [JsonInclude]
    public Address Address { get; private set; }
    [JsonInclude]
    public decimal TotalCapacityWeight { get; private set; }
    [JsonInclude]
    public decimal TotalCapacityVolume { get; private set; }
    [JsonInclude]
    public string OperatingHours { get; private set; }
    [JsonInclude]
    public string ContactPhone { get; private set; }
    [JsonInclude]
    public string ContactEmail { get; private set; }
    [JsonInclude]
    public bool IsActive { get; private set; }

    private readonly List<Room> _rooms = new();
    public IReadOnlyCollection<Room> Rooms => _rooms.AsReadOnly();

    private readonly List<Dock> _docks = new();
    public IReadOnlyCollection<Dock> Docks => _docks.AsReadOnly();

    private readonly List<YardSpot> _yardSpots = new();
    public IReadOnlyCollection<YardSpot> YardSpots => _yardSpots.AsReadOnly();

    [JsonConstructor]
    private Warehouse() : base(Guid.Empty)
    {
        Name = null!;
        Address = null!;
        OperatingHours = null!;
        ContactPhone = null!;
        ContactEmail = null!;
    }

    private Warehouse(Guid id, Guid companyId, string name, Address address) : base(id)
    {
        CompanyId = companyId;
        Name = name;
        Address = address;
        IsActive = true;    
        OperatingHours = "N/A";  
        ContactPhone = "N/A";  
        ContactEmail = "N/A";  
    }

    public static Warehouse Create(Guid companyId, string name, Address address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Warehouse name cannot be empty.", nameof(name));

        return new Warehouse(Guid.NewGuid(), companyId, name, address);
    }

    public YardSpot? AssignYardSpot(Guid truckId)
    {
        var availableSpot = _yardSpots.FirstOrDefault(ys => ys.Status == YardSpotStatus.Available);
        availableSpot?.Occupy(truckId);
        return availableSpot;
    }

    public void Update(
        string name,
        Address address,
        string contactPhone,
        string contactEmail,
        string operatingHours,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Warehouse name cannot be empty.", nameof(name));

        Name = name;
        Address = address ?? throw new ArgumentNullException(nameof(address));
        ContactPhone = contactPhone ?? "N/A";
        ContactEmail = contactEmail ?? "N/A";
        OperatingHours = operatingHours ?? "N/A";
        IsActive = isActive;
    }

    public Dock AddDock(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Dock name cannot be empty.", nameof(name));
        if (_docks.Any(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A dock with the name '{name}' already exists.");

        var newDock = new Dock(Guid.NewGuid(), this.Id, name, DockType.Both); // Default to Both
        _docks.Add(newDock);
        return newDock;
    }

    public YardSpot AddYardSpot(string spotNumber)
    {
        if (string.IsNullOrWhiteSpace(spotNumber))
            throw new ArgumentException("Spot number cannot be empty.", nameof(spotNumber));
        if (_yardSpots.Any(ys => ys.SpotNumber.Equals(spotNumber, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A yard spot with the number '{spotNumber}' already exists.");

        var newSpot = new YardSpot(Guid.NewGuid(), this.Id, spotNumber);
        _yardSpots.Add(newSpot);
        return newSpot;
    }
}