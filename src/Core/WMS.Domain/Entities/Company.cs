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

    public Company() : base(Guid.Empty)
    {
        Name = null!;
        TaxId = null!;
        Address = null!;
        PhoneNumber = null!;
        Email = null!;
        Website = null!;
        SubscriptionPlan = null!;
    }
}