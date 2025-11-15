namespace WMS.Domain.Shared;

public abstract class AuditableEntity<TId> : Entity<TId> where TId : notnull
{
    public DateTime CreatedOn { get; protected set; }
    public Guid? CreatedBy { get; protected set; }
    public DateTime? LastModifiedOn { get; protected set; }
    public Guid? LastModifiedBy { get; protected set; }

    protected AuditableEntity(TId id) : base(id) { }

#pragma warning disable CS8618
    protected AuditableEntity() { }
#pragma warning restore CS8618
}
