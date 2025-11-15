using WMS.Domain.Aggregates.DockAppointment;
using WMS.Domain.Entities;   
using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities.Transaction;

public class WithdrawalTransaction : Entity<Guid>
{
    public Guid AccountId { get; private set; }
    public string ShipmentNumber { get; private set; }
    public Guid? AppointmentId { get; private set; }
    public decimal TotalWeight { get; private set; }
    public DateTime Timestamp { get; private set; }
    public ShipmentStatus Status { get; private set; }

    public DockAppointment? Appointment { get; private set; }
    public Account Account { get; private set; } = null!;  

    private readonly List<PickTransaction> _picks = new();
    public IReadOnlyCollection<PickTransaction> Picks => _picks.AsReadOnly();

    private WithdrawalTransaction() : base(Guid.Empty)
    {
        ShipmentNumber = null!;
    }

    public static WithdrawalTransaction Create(Guid appointmentId, string shipmentNumber, IEnumerable<PickTransaction> picks)
    {
        var pickList = picks.ToList();
        var firstPick = pickList.FirstOrDefault() ?? throw new InvalidOperationException("Cannot create a withdrawal with no picked items.");

        var withdrawal = new WithdrawalTransaction
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            ShipmentNumber = shipmentNumber,
            AccountId = firstPick.AccountId,
            TotalWeight = pickList.Sum(p => p.Quantity * p.MaterialInventory.WeightActual.Value),
            Timestamp = DateTime.UtcNow,
            Status = ShipmentStatus.Packed
        };
        withdrawal._picks.AddRange(pickList);
        return withdrawal;
    }
}