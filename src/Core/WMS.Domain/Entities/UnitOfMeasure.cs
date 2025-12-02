using System.Text.Json.Serialization;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities;

public class UnitOfMeasure(Guid id, string name, string symbol) : Entity<Guid>(id)
{
    [JsonInclude]
    public string Name { get; private set; } = name;
    [JsonInclude]
    public string Symbol { get; private set; } = symbol;

    [JsonConstructor]
    private UnitOfMeasure() : this(Guid.Empty, string.Empty, string.Empty) { }

    public static UnitOfMeasure Create(string name, string symbol)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("UOM name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("UOM symbol cannot be empty.", nameof(symbol));

        return new UnitOfMeasure(Guid.NewGuid(), name, symbol);
    }

    public void Update(string name, string symbol, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("UOM name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("UOM symbol cannot be empty.", nameof(symbol));

        Name = name;
        Symbol = symbol;
    }
}