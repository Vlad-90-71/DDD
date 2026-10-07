namespace DDD.Domain.Common.Events;

public abstract class ServiceEventHandler<TEvent>(Func<TEvent, CancellationToken, Task> action)
    : IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken) =>
        action(domainEvent, cancellationToken);
}