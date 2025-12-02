using System.Text.Json.Serialization;
using WMS.Domain.Shared;

namespace WMS.Domain.ValueObjects;

public class TemperatureRange : ValueObject
{
    [JsonInclude]
    public decimal MinTemperature { get; private set; }
    [JsonInclude]
    public decimal MaxTemperature { get; private set; }

    [JsonConstructor]
    private TemperatureRange() { }

    private TemperatureRange(decimal minTemperature, decimal maxTemperature)
    {
        MinTemperature = minTemperature;
        MaxTemperature = maxTemperature;
    }

    public static TemperatureRange Create(decimal min, decimal max)
    {
        if (min > max)
        {
            throw new ArgumentException("Minimum temperature cannot be greater than maximum temperature.");
        }
        return new TemperatureRange(min, max);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return MinTemperature;
        yield return MaxTemperature;
    }
}
