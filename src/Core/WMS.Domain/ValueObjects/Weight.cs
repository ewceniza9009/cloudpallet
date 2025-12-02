using System.Text.Json.Serialization;
using WMS.Domain.Shared;

namespace WMS.Domain.ValueObjects;

public class Weight : ValueObject
{
    [JsonInclude]
    public decimal Value { get; private set; }
    [JsonInclude]
    public string Unit { get; private set; }

    [JsonConstructor]
    private Weight()
    {
        Value = 0;
        Unit = string.Empty;
    }

    private Weight(decimal value, string unit)
    {
        Value = value;
        Unit = unit;
    }

    public static Weight Create(decimal value, string unit)
    {
        if (value < 0)
        {
            throw new ArgumentException("Weight cannot be negative.", nameof(value));
        }
        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("Weight unit must be specified.", nameof(unit));
        }
        return new Weight(value, unit);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return Unit;
    }
}