using System.Text.Json.Serialization;
using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Domain.Aggregates.Warehouse;

public class YardSpot : Entity<Guid>
{
    [JsonInclude]
    public Guid WarehouseId { get; private set; }
    [JsonInclude]
    public string SpotNumber { get; private set; }
    [JsonInclude]
    public YardSpotStatus Status { get; private set; }
    [JsonInclude]
    public Guid? CurrentTruckId { get; private set; }  
    [JsonInclude]
    public DateTime? OccupiedSince { get; private set; }  
    [JsonInclude]
    public decimal CapacityTruckLength { get; private set; }
    [JsonInclude]
    public bool IsActive { get; private set; }

    [JsonConstructor]
    private YardSpot() : base(Guid.Empty)
    {
        SpotNumber = null!;
    }

    internal YardSpot(Guid id, Guid warehouseId, string spotNumber) : base(id)
    {
        WarehouseId = warehouseId;
        SpotNumber = spotNumber;
        Status = YardSpotStatus.Available;
        IsActive = true;
    }

    public void Occupy(Guid truckId)
    {
        if (Status != YardSpotStatus.Available)
        {
            throw new InvalidOperationException("Yard spot is not available.");
        }
        Status = YardSpotStatus.Occupied;
        CurrentTruckId = truckId;
        OccupiedSince = DateTime.UtcNow;
    }

    public void Vacate()
    {
        Status = YardSpotStatus.Available;
        CurrentTruckId = null;
        OccupiedSince = null;
    }

    public void Update(string spotNumber, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(spotNumber))
        {
            throw new ArgumentException("Spot number cannot be empty.", nameof(spotNumber));
        }

        if (!IsActive && isActive) // Reactivating
        {
            Status = YardSpotStatus.Available; // Ensure it's available when activated
        }
        else if (IsActive && !isActive) // Deactivating
        {
            if (Status == YardSpotStatus.Occupied)
            {
                throw new InvalidOperationException("Cannot deactivate an occupied yard spot.");
            }
            Status = YardSpotStatus.Reserved; // Or another "out of service" status
        }

        SpotNumber = spotNumber;
        IsActive = isActive;
    }
}