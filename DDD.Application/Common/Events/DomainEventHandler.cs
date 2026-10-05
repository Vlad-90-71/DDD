using DDD.Domain.Common.Events;
using DDD.Eventing.Contracts;

namespace DDD.Application.Common.Events;

public sealed class DomainEventHandler<TEvent>(IEventProcessingStore eventStore)
    : IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    public async Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken)
    {
        if (await eventStore.IsProcessedAsync(domainEvent.EventId, cancellationToken))
            return;

        eventStore.LogProcessed(domainEvent);

        eventStore.MarkAsProcessed(domainEvent.EventId);
    }
}