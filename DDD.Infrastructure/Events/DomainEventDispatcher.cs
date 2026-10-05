using DDD.Domain.Common.Events;
using DDD.Eventing.Contracts;

namespace DDD.Infrastructure.Events;

public sealed class DomainEventDispatcher(
    IEnumerable<IDomainEventHandler> handlers,
    IEventProcessingStore eventStore)
    : IDomainEventDispatcher
{
    public async Task<EventProcessingClaimResult> DispatchAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var matchingHandlers = handlers
            .Where(x => x.EventType == domainEvent.GetType())
            .ToArray();

        if (matchingHandlers.Length == 0)
        {
            return new EventProcessingClaimResult(
                EventProcessingClaimStatus.NoHandler,
                domainEvent.EventId,
                null);
        }

        var claim = await eventStore.TryClaimAsync(
            domainEvent.EventId,
            cancellationToken);

        if (claim.Status != EventProcessingClaimStatus.Claimed)
            return claim;

        foreach (var handler in matchingHandlers)
        {
            await handler.HandleAsync(
                domainEvent,
                cancellationToken);
        }

        eventStore.LogProcessed(domainEvent);

        await eventStore.MarkAsProcessedAsync(
            domainEvent.EventId,
            claim.ClaimToken!.Value,
            cancellationToken);

        return claim;
    }
}