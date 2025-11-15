using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities;

public class Invoice : AggregateRoot<Guid>
{
    public Guid AccountId { get; private set; }
    public Account Account { get; private set; } = null!;
    public string InvoiceNumber { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public DateTime DueDate { get; private set; }
    public int GracePeriodDays { get; private set; }
    public string? PdfPath { get; private set; }

    private readonly List<InvoiceLine> _lines = new();
    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();

    private Invoice() : base(Guid.Empty)
    {
        InvoiceNumber = null!;
    }

    private Invoice(Guid id, Guid accountId, DateTime periodStart, DateTime periodEnd) : base(id)
    {
        AccountId = accountId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        InvoiceNumber = $"INV-{DateTime.UtcNow.Ticks}";
        Status = InvoiceStatus.Draft;
        GracePeriodDays = 30;
        DueDate = DateTime.UtcNow.AddDays(GracePeriodDays);
    }

    public static Invoice Create(Guid accountId, DateTime periodStart, DateTime periodEnd)
    {
        return new Invoice(Guid.NewGuid(), accountId, periodStart, periodEnd);
    }

    public void AddLine(Guid? materialId, ServiceType serviceType, decimal quantity, decimal unitRate, string description, string tier)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Cannot add lines to an invoice that is no longer in Draft status.");

        var line = InvoiceLine.Create(this.Id, materialId, serviceType, quantity, unitRate, description, tier);
        _lines.Add(line);
        RecalculateTotal();
    }

    public void FinalizeInvoice(decimal taxRate = 0.12m)
    {
        if (Status != InvoiceStatus.Draft) return;

        Status = InvoiceStatus.Issued;
        // compute tax from subtotal rather than mutating multiple times
        TaxAmount = _lines.Sum(l => l.Amount) * taxRate;
        TotalAmount = _lines.Sum(l => l.Amount) + TaxAmount;
    }

    private void RecalculateTotal()
    {
        TotalAmount = _lines.Sum(line => line.Amount);
    }
}