using System.Text.Json.Serialization;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities;

public class PalletType : AuditableEntity<Guid>    
{
    [JsonInclude]
    public string Name { get; private set; }
    [JsonInclude]
    public decimal TareWeight { get; private set; }
    [JsonInclude]
    public decimal Length { get; private set; }
    [JsonInclude]
    public decimal Width { get; private set; }
    [JsonInclude]
    public decimal Height { get; private set; }
    [JsonInclude]
    public bool IsActive { get; private set; }

    [JsonConstructor]
    private PalletType() : base(Guid.Empty)    
    {
        Name = null!;     
    }

    public static PalletType Create(string name, decimal tareWeight, decimal length, decimal width, decimal height)
    {
        ValidateInput(name, tareWeight, length, width, height);
        return new PalletType
        {
            Id = Guid.NewGuid(),
            Name = name,
            TareWeight = tareWeight,
            Length = length,
            Width = width,
            Height = height,
            IsActive = true      
        };
    }

    public void Update(string name, decimal tareWeight, decimal length, decimal width, decimal height, bool isActive)
    {
        ValidateInput(name, tareWeight, length, width, height);
        Name = name;
        TareWeight = tareWeight;
        Length = length;
        Width = width;
        Height = height;
        IsActive = isActive;
    }

    private static void ValidateInput(string name, decimal tareWeight, decimal length, decimal width, decimal height)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Pallet Type name cannot be empty.", nameof(name));
        if (tareWeight < 0)
            throw new ArgumentException("Tare Weight cannot be negative.", nameof(tareWeight));
        if (length <= 0 || width <= 0 || height <= 0)
            throw new ArgumentException("Dimensions (Length, Width, Height) must be positive.", nameof(length));
    }
}