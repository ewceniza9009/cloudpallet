namespace WMS.Domain.Shared;

public abstract class AggregateRoot<TId> : AuditableEntity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    protected AggregateRoot(TId id) : base(id) { }

#pragma warning disable CS8618
    protected AggregateRoot() { }
#pragma warning restore CS8618
}
