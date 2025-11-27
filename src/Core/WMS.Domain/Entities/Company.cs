using WMS.Domain.Enums;
using WMS.Domain.Shared;
using WMS.Domain.ValueObjects;

namespace WMS.Domain.Entities;

public class Company : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string TaxId { get; private set; }
    public Address Address { get; private set; }
    public string PhoneNumber { get; private set; }
    public string Email { get; private set; }
    public string Website { get; private set; }
    public CompanyStatus Status { get; private set; }
    public string SubscriptionPlan { get; private set; }
    public string Gs1CompanyPrefix { get; private set; }
    public string DefaultBarcodeFormat { get; private set; }

    public bool IsPickingWeightReadonly { get; private set; }

    public Company() : base(Guid.Empty)
    {
        Name = null!;
        TaxId = null!;
        Address = null!;
        PhoneNumber = null!;
        Email = null!;
        Website = null!;
        SubscriptionPlan = null!;
        Gs1CompanyPrefix = "0000000"; // Default dummy prefix
        DefaultBarcodeFormat = "SSCC-18";
        IsPickingWeightReadonly = false;
    }

    public void UpdateGs1Settings(string prefix, string format)
    {
        Gs1CompanyPrefix = prefix;
        DefaultBarcodeFormat = format;
    }

    public void UpdatePickingSettings(bool isReadonly)
    {
        IsPickingWeightReadonly = isReadonly;
    }
}