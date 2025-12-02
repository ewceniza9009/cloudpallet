using System.Text.Json.Serialization;
using WMS.Domain.Shared;
using WMS.Domain.ValueObjects;

namespace WMS.Domain.Entities;

public class Supplier : AggregateRoot<Guid>
{
    [JsonInclude]
    public string Name { get; private set; }
    [JsonInclude]
    public string Description { get; private set; }
    [JsonInclude]
    public Address Address { get; private set; }
    [JsonInclude]
    public string ContactName { get; private set; }
    [JsonInclude]
    public string Phone { get; private set; }
    [JsonInclude]
    public string Email { get; private set; }
    [JsonInclude]
    public string TaxId { get; private set; }
    [JsonInclude]
    public int LeadTimeDays { get; private set; }
    [JsonInclude]
    public bool CertificationColdChain { get; private set; }
    [JsonInclude]
    public string PaymentTerms { get; private set; }
    [JsonInclude]
    public string CurrencyCode { get; private set; }
    [JsonInclude]
    public bool IsActive { get; private set; }
    [JsonInclude]
    public decimal CreditLimit { get; private set; }

    [JsonConstructor]
    private Supplier() : base(Guid.Empty)
    {
        Name = null!;
        Description = null!;
        Address = null!;
        ContactName = null!;
        Phone = null!;
        Email = null!;
        TaxId = null!;
        PaymentTerms = null!;
        CurrencyCode = null!;
    }

    private Supplier(Guid id, string name) : base(id)
    {
        Name = name;
        IsActive = true;
        Description = string.Empty;
        Address = new Address("", "", "", "", "");    
        ContactName = string.Empty;
        Phone = string.Empty;
        Email = string.Empty;
        TaxId = string.Empty;
        PaymentTerms = "N/A";
        CurrencyCode = "PHP";
    }

    public static Supplier Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Supplier name cannot be empty.", nameof(name));
        return new Supplier(Guid.NewGuid(), name);
    }
    public void UpdateGeneral(
        string name,
        string description,
        string taxId,
        int leadTimeDays,
        string paymentTerms,
        decimal creditLimit,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Supplier name cannot be empty.", nameof(name));

        Name = name;
        Description = description;
        TaxId = taxId;
        LeadTimeDays = leadTimeDays;
        PaymentTerms = paymentTerms;
        CreditLimit = creditLimit;
        IsActive = isActive;
    }

    public void UpdateContactInfo(
        string contactName,
        string phone,
        string email,
        Address address)
    {
        ContactName = contactName;
        Phone = phone;
        Email = email;
        Address = address;
    }
}