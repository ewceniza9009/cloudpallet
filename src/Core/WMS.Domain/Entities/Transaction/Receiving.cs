using WMS.Domain.Aggregates.DockAppointment;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities.Transaction;

public class Receiving : AggregateRoot<Guid>
{
    public Guid SupplierId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public decimal TotalWeight { get; private set; }
    public DateTime Timestamp { get; private set; }
    public ReceivingStatus Status { get; private set; }
    public int TotalPallets { get; private set; }
    public Guid? AccountId { get; private set; }
    public string? Remarks { get; private set; }

    public Supplier Supplier { get; private set; } = null!;
    public DockAppointment Appointment { get; private set; } = null!;
    public Account? Account { get; private set; }  
    public User? CreatedByUser { get; private set; }  

    private readonly List<Pallet> _pallets = new();
    public IReadOnlyCollection<Pallet> Pallets => _pallets.AsReadOnly();

    private Receiving() : base(Guid.Empty) { }

    private Receiving(Guid id, Guid supplierId, Guid appointmentId, Guid? accountId, string? remarks) : base(id)
    {
        SupplierId = supplierId;
        AppointmentId = appointmentId;
        AccountId = accountId;
        Remarks = remarks;
        Timestamp = DateTime.UtcNow;
        Status = ReceivingStatus.InProgress;
        TotalWeight = 0;
        TotalPallets = 0;
    }

    public static Receiving Create(Guid supplierId, Guid appointmentId, Guid? accountId, string? remarks = null)
    {
        return new Receiving(Guid.NewGuid(), supplierId, appointmentId, accountId, remarks);
    }

    public void IncrementPalletCount()
    {
        TotalPallets++;
    }

    public void DecrementPalletCount()
    {
        if (TotalPallets > 0)
        {
            TotalPallets--;
        }
    }

    public void Complete()
    {
        if (Status == ReceivingStatus.Completed) return;
        Status = ReceivingStatus.Completed;
    }
}