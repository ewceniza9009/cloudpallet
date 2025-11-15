using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities;

public class InvoiceLine : Entity<Guid>
{
    public Guid InvoiceId { get; private set; }
    public Guid? MaterialId { get; private set; }
    public ServiceType ServiceType { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitRate { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; }
    public string Tier { get; private set; }

    private InvoiceLine() : base(Guid.Empty)
    {
        Description = null!;
        Tier = null!;
    }

    internal static InvoiceLine Create(Guid invoiceId, Guid? materialId, ServiceType serviceType, decimal quantity, decimal unitRate, string description, string tier)
    {
        var amount = quantity * unitRate;
        return new InvoiceLine(Guid.NewGuid(), invoiceId, materialId, serviceType, quantity, unitRate, amount, description, tier);
    }

    private InvoiceLine(Guid id, Guid invoiceId, Guid? materialId, ServiceType serviceType, decimal quantity, decimal unitRate, decimal amount, string description, string tier) : base(id)
    {
        InvoiceId = invoiceId;
        MaterialId = materialId;
        ServiceType = serviceType;
        Quantity = quantity;
        UnitRate = unitRate;
        Amount = amount;
        Description = description;
        Tier = tier;
    }
}