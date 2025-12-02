using System.Text.Json.Serialization;
using WMS.Domain.Enums;
using WMS.Domain.Shared;

namespace WMS.Domain.Entities;

public class Rate : Entity<Guid>
{
    [JsonInclude]
    public Guid? AccountId { get; private set; }    
    [JsonInclude]
    public ServiceType ServiceType { get; private set; }
    [JsonInclude]
    public RateUom Uom { get; private set; }
    [JsonInclude]
    public decimal Value { get; private set; }
    [JsonInclude]
    public string Tier { get; private set; }
    [JsonInclude]
    public DateTime EffectiveStartDate { get; private set; }
    [JsonInclude]
    public DateTime? EffectiveEndDate { get; private set; }    
    [JsonInclude]
    public decimal? MinQuantity { get; private set; }
    [JsonInclude]
    public bool IsActive { get; private set; }

    [JsonConstructor]
    private Rate() : base(Guid.Empty)
    {
        Tier = null!;
    }

    public static Rate Create(Guid? accountId, ServiceType serviceType, RateUom uom, decimal value, string tier, DateTime effectiveStartDate, DateTime? effectiveEndDate)
    {
        return new Rate(
            Guid.NewGuid(),
            accountId,
            serviceType,
            uom,
            value,
            tier,
            effectiveStartDate,
            effectiveEndDate ?? new DateTime(2099, 12, 31)       
        );
    }

    private Rate(Guid id, Guid? accountId, ServiceType serviceType, RateUom uom, decimal value, string tier, DateTime effectiveStartDate, DateTime effectiveEndDate) : base(id)
    {
        AccountId = accountId;
        ServiceType = serviceType;
        Uom = uom;
        Value = value;
        Tier = tier;
        EffectiveStartDate = effectiveStartDate;
        EffectiveEndDate = effectiveEndDate;
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
        EffectiveEndDate = DateTime.UtcNow;
    }
}