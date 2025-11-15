using WMS.Domain.Enums;
using WMS.Domain.Shared;
using WMS.Domain.ValueObjects;

namespace WMS.Domain.Entities;

public class Account : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public AccountType TypeId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Address Address { get; private set; }
    public string ContactName { get; private set; }
    public string Phone { get; private set; }
    public string Email { get; private set; }
    public string TaxId { get; private set; }
    public decimal CreditLimit { get; private set; }
    public string PaymentTerms { get; private set; }
    public string CurrencyCode { get; private set; }
    public TempZone? PreferredTempZone { get; private set; }
    public bool IsPreferred { get; private set; }
    public bool IsActive { get; private set; }

    private Account() : base(Guid.Empty)
    {
        Name = null!;
        Address = null!;
        ContactName = null!;
        Phone = null!;
        Email = null!;
        TaxId = null!;
        PaymentTerms = null!;
        CurrencyCode = null!;
    }

    private Account(Guid id, string name, AccountType type) : base(id)
    {
        Name = name;
        TypeId = type;
        IsActive = true;
        Address = new Address("", "", "", "", "");    
        ContactName = string.Empty;
        Phone = string.Empty;
        Email = string.Empty;
        TaxId = string.Empty;
        PaymentTerms = "N/A";
        CurrencyCode = "PHP";
        CreditLimit = 0;
    }

    public static Account Create(string name, AccountType type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name cannot be empty.", nameof(name));

        return new Account(Guid.NewGuid(), name, type);
    }
    public void UpdateGeneral(
        string name,
        AccountType typeId,
        string taxId,
        string paymentTerms,
        decimal creditLimit,
        bool isActive,
        Guid? categoryId,
        TempZone? preferredTempZone,
        bool isPreferred)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name cannot be empty.", nameof(name));

        Name = name;
        TypeId = typeId;
        TaxId = taxId;
        PaymentTerms = paymentTerms;
        CreditLimit = creditLimit;
        IsActive = isActive;
        CategoryId = categoryId;
        PreferredTempZone = preferredTempZone;
        IsPreferred = isPreferred;
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