using System.Text.Json.Serialization;
using WMS.Domain.Shared;

namespace WMS.Domain.ValueObjects;

public class Address : ValueObject
{
    [JsonInclude]
    public string Street { get; private set; }
    [JsonInclude]
    public string City { get; private set; }
    [JsonInclude]
    public string State { get; private set; }
    [JsonInclude]
    public string PostalCode { get; private set; }
    [JsonInclude]
    public string Country { get; private set; }

    [JsonConstructor]
    private Address()
    {
        Street = string.Empty;
        City = string.Empty;
        State = string.Empty;
        PostalCode = string.Empty;
        Country = string.Empty;
    }

    public Address(string street, string city, string state, string postalCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
    }
}