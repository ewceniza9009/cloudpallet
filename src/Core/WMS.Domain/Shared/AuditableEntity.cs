using System.Text.Json.Serialization;

namespace WMS.Domain.Shared;

public abstract class AuditableEntity<TId> : Entity<TId> where TId : notnull
{
    [JsonInclude]
    public DateTime CreatedOn { get; protected set; }
    [JsonInclude]
    public Guid? CreatedBy { get; protected set; }
    [JsonInclude]
    public DateTime? LastModifiedOn { get; protected set; }
    [JsonInclude]
    public Guid? LastModifiedBy { get; protected set; }

    protected AuditableEntity(TId id) : base(id) { }

#pragma warning disable CS8618
    protected AuditableEntity() { }
#pragma warning restore CS8618
}
