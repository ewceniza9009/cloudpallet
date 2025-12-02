using WMS.Domain.Enums;
using WMS.Domain.Shared;
using WMS.Domain.ValueObjects;

namespace WMS.Domain.Entities;

public class Company : AggregateRoot<Guid>
{
    public string Name { get; set; }
    public string TaxId { get; set; }
    public Address Address { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string Website { get; set; }
    public CompanyStatus Status { get; set; }
    public string SubscriptionPlan { get; set; }
    public string Gs1CompanyPrefix { get; set; }
    public string DefaultBarcodeFormat { get; set; }

    public bool IsPickingWeightReadonly { get; set; }

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