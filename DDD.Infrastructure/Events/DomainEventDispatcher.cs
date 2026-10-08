using DDD.Domain.Common.Events;
using DDD.Eventing.Contracts;
using Microsoft.Extensions.Logging;

namespace DDD.Infrastructure.Events;

public sealed class DomainEventDispatcher(
    IEnumerable<IDomainEventHandler> handlers,
    IEventProcessingStore eventStore,
    ILogger<DomainEventDispatcher> logger)
    : IDomainEventDispatcher
{
    public async Task<EventProcessingClaimResult> DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType().Name;

        logger.LogDebug(
            "Dispatching event. EventId={EventId}, Type={EventType}",
            domainEvent.EventId, eventType);

        var matchingHandlers = handlers
            .Where(x => x.EventType == domainEvent.GetType())
            .ToArray();

        logger.LogDebug(
            "Found {HandlerCount} handler(s). EventId={EventId}, Type={EventType}",
            matchingHandlers.Length, domainEvent.EventId, eventType);

        if (matchingHandlers.Length == 0)
        {
            logger.LogWarning(
                "No handler found. EventId={EventId}, Type={EventType}",
                domainEvent.EventId, eventType);

            return new EventProcessingClaimResult(
                EventProcessingClaimStatus.NoHandler,
                domainEvent.EventId,
                null);
        }

        var claim = await eventStore.TryClaimAsync(domainEvent.EventId, cancellationToken);

        if (claim.Status != EventProcessingClaimStatus.Claimed)
        {
            logger.LogDebug(
                "Event processing skipped. EventId={EventId}, Type={EventType}, Status={Status}",
                domainEvent.EventId, eventType, claim.Status);

            return claim;
        }

        try
        {
            foreach (var handler in matchingHandlers)
            {
                logger.LogDebug(
                    "Executing handler {HandlerType}. EventId={EventId}, Type={EventType}",
                    handler.GetType().Name, domainEvent.EventId, eventType);

                await handler.HandleAsync(domainEvent, cancellationToken);

                logger.LogDebug(
                    "Handler {HandlerType} completed. EventId={EventId}, Type={EventType}",
                    handler.GetType().Name, domainEvent.EventId, eventType);
            }

            await eventStore.MarkAsProcessedAsync(domainEvent.EventId, claim.ClaimToken!.Value, cancellationToken);

            logger.LogInformation(
                "Event processed successfully. EventId={EventId}, Type={EventType}",
                domainEvent.EventId, eventType);

            return claim;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Event processing failed. EventId={EventId}, Type={EventType}",
                domainEvent.EventId,eventType);

            var failed = await eventStore.ReleaseClaimAsync(
                domainEvent.EventId,
                claim.ClaimToken!.Value,
                ex.ToString(),
                cancellationToken);

            return new EventProcessingClaimResult(
                failed
                    ? EventProcessingClaimStatus.Failed
                    : EventProcessingClaimStatus.Retry,
                domainEvent.EventId,
                null);
        }
    }
}