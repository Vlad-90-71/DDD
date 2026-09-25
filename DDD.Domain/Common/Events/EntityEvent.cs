using System.Collections.ObjectModel;

namespace DDD.Domain.Common.Events;

public interface IEntityEvent
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

public abstract class EntityEvent : IEntityEvent
{
    private readonly List<IDomainEvent> _domainEvents = [];
    private readonly ReadOnlyCollection<IDomainEvent> _domainEventsReadOnly;

    public EntityEvent() => _domainEventsReadOnly = _domainEvents.AsReadOnly();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEventsReadOnly;

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
