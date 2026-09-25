using DDD.Domain.Common.Events;

namespace DDD.Eventing.Contracts;

public interface IDomainEventHandler
{
    Type EventType { get; }

    Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
public interface IDomainEventHandler<in TEvent> : IDomainEventHandler where TEvent : IDomainEvent
{
    Type IDomainEventHandler.EventType => typeof(TEvent);

    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);

    async Task IDomainEventHandler.HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken) =>
        await HandleAsync((TEvent)domainEvent, cancellationToken);
}
