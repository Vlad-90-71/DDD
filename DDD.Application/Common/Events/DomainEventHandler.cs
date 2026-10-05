using DDD.Domain.Common.Events;
using DDD.Eventing.Contracts;

namespace DDD.Application.Common.Events;

public sealed class DomainEventHandler<TEvent> : IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}