using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace DDD.Domain.Common.Events;

public interface IEntityEvent
{
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
public abstract class EntityEvent : EntityEvent<int> { }
public abstract class EntityEvent<TKey> : Entity<TKey>, IEntityEvent where TKey : struct
{
    // Вместо List<T> мы сразу храним данные в ImmutableList.
    // ImmutableList.Empty инициализирует пустой список без выделения лишней памяти (синглтон).
    private ImmutableList<DomainEvent> _domainEvents = [];

    protected EntityEvent() { }

    // Отдаем коллекцию наружу. Никакие касты к List<T> больше не сработают.
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents;

    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        // ВАЖНО: Метод .Add() не меняет текущий список, а возвращает НОВЫЙ.
        // Поэтому мы переприсваиваем ссылку нашей переменной.
        _domainEvents = _domainEvents.Add(domainEvent);
    }

    // Явная реализация интерфейса скрывает метод очистки от бизнес-логики.
    // Присваиваем пустой синглтон — память мгновенно освобождается.
    void IEntityEvent.ClearDomainEvents() => _domainEvents = [];
}
/*
public abstract class EntityEvent<TKey> : Entity<TKey>, IEntityEvent where TKey : struct
{
    private readonly List<DomainEvent> _domainEvents = [];
    private readonly ReadOnlyCollection<DomainEvent> _domainEventsReadOnly;

    protected EntityEvent() => _domainEventsReadOnly = _domainEvents.AsReadOnly();

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEventsReadOnly;

    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
    void IEntityEvent.ClearDomainEvents() => _domainEvents.Clear();
}
*/