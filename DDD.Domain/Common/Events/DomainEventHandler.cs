namespace DDD.Domain.Common.Events;

public interface IDomainEventHandler
{
    Type EventType { get; }
    Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
public interface IDomainEventHandler<in TEvent> : IDomainEventHandler where TEvent : IDomainEvent
{
    Type IDomainEventHandler.EventType => typeof(TEvent);
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
    Task IDomainEventHandler.HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken) =>
        HandleAsync((TEvent)domainEvent, cancellationToken);
}

public sealed class DomainEventHandler<TEvent> : IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}