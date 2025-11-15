using WMS.Domain.Shared;

namespace WMS.Domain.Entities;

public class Truck : AggregateRoot<Guid>
{
    public Guid CarrierId { get; private set; }
    public string LicensePlate { get; private set; }
    public string Model { get; private set; }
    public decimal CapacityWeight { get; private set; }
    public decimal CapacityVolume { get; private set; }
    public bool IsActive { get; private set; }

    public Carrier Carrier { get; private set; } = null!;

    private Truck() : base(Guid.Empty)
    {
        LicensePlate = null!;
        Model = null!;
    }

    private Truck(Guid id, Guid carrierId, string licensePlate, string model, decimal capacityWeight, decimal capacityVolume) : base(id)
    {
        CarrierId = carrierId;
        LicensePlate = licensePlate;
        Model = model;
        CapacityWeight = capacityWeight;
        CapacityVolume = capacityVolume;
        IsActive = true;
    }

    public static Truck Create(Guid carrierId, string licensePlate, string model, decimal capacityWeight, decimal capacityVolume)
    {
        if (carrierId == Guid.Empty) throw new ArgumentException("Carrier ID is required.", nameof(carrierId));
        if (string.IsNullOrWhiteSpace(licensePlate)) throw new ArgumentException("License plate cannot be empty.", nameof(licensePlate));

        return new Truck(Guid.NewGuid(), carrierId, licensePlate, model, capacityWeight, capacityVolume);
    }

    public void Update(string licensePlate, string model, decimal capacityWeight, decimal capacityVolume, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(licensePlate)) throw new ArgumentException("License plate cannot be empty.", nameof(licensePlate));

        LicensePlate = licensePlate;
        Model = model;
        CapacityWeight = capacityWeight;
        CapacityVolume = capacityVolume;
        IsActive = isActive;
    }
}