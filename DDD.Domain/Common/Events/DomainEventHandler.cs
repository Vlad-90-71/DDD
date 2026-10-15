namespace DDD.Domain.Common.Events;

public interface IDomainEventHandler
{
    Type EventType { get; }
    TimeSpan? ClaimedUntilUtc { get; }
    Task HandleAsync(DomainEvent domainEvent, CancellationToken cancellationToken);
}
public interface IDomainEventHandler<in TEvent> : IDomainEventHandler where TEvent : DomainEvent
{
    Type IDomainEventHandler.EventType => typeof(TEvent);
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
    Task IDomainEventHandler.HandleAsync(DomainEvent domainEvent, CancellationToken cancellationToken) =>
        HandleAsync((TEvent)domainEvent, cancellationToken);
}

public abstract class DomainEventHandler<TEvent>(Func<TEvent, CancellationToken, Task> action)
    : IDomainEventHandler<TEvent> where TEvent : DomainEvent
{
    public virtual TimeSpan? ClaimedUntilUtc => null;

    public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken) =>
        action(domainEvent, cancellationToken);
}

public sealed class DefaultDomainEventHandler<TEvent>()
    : DomainEventHandler<TEvent>((_, _) => Task.CompletedTask) where TEvent : DomainEvent { }

