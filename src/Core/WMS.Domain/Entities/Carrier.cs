using WMS.Domain.Shared;
using WMS.Domain.ValueObjects;

namespace WMS.Domain.Entities;

public class Carrier : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string ScacCode { get; private set; }
    public string? DotNumber { get; private set; }
    public string? ContactName { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? ContactEmail { get; private set; }
    public Address? Address { get; private set; }
    public bool CertificationColdChain { get; private set; }
    public string? InsurancePolicyNumber { get; private set; }
    public DateTime? InsuranceExpiryDate { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<Truck> _trucks = new();
    public IReadOnlyCollection<Truck> Trucks => _trucks.AsReadOnly();

    private Carrier(Guid id, string name, string scacCode) : base(id)
    {
        Name = name;
        ScacCode = scacCode;
        IsActive = true;
    }

#pragma warning disable CS8618
    private Carrier() : base(Guid.Empty) { }
#pragma warning restore CS8618

    public static Carrier Create(string name, string scacCode)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Carrier name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(scacCode))
            throw new ArgumentException("SCAC code cannot be empty.", nameof(scacCode));

        return new Carrier(Guid.NewGuid(), name, scacCode);
    }

    public void UpdateDetails(string name, string scacCode)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Carrier name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(scacCode))
            throw new ArgumentException("SCAC code cannot be empty.", nameof(scacCode));

        Name = name;
        ScacCode = scacCode;
    }
    public void UpdateContactInfo(string? contactName, string? contactPhone, string? contactEmail, Address? address)
    {
        ContactName = contactName;
        ContactPhone = contactPhone;
        ContactEmail = contactEmail;
        Address = address;
    }

    public void SetInsuranceDetails(string? policyNumber, DateTime? expiryDate)
    {
        InsurancePolicyNumber = policyNumber;
        InsuranceExpiryDate = expiryDate;
    }

    public void SetCertification(bool isCertified)
    {
        CertificationColdChain = isCertified;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}