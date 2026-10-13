using Microsoft.Extensions.Logging;
using DDD.Domain.Common.Events;
using DDD.Eventing.Contracts;

namespace DDD.Infrastructure.Events;

public sealed class DomainEventDispatcher(
    IEnumerable<IDomainEventHandler> handlers,
    IEventProcessingStore eventStore,
    ILogger<DomainEventDispatcher> logger,
    IDomainEventMetrics metrics)
    : IDomainEventDispatcher
{
    public async Task<EventProcessingClaimResult> DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventId = domainEvent.EventId;
        var eventType = domainEvent.GetType().Name;

        var matchingHandlers = handlers.Where(x => x.EventType == domainEvent.GetType()).ToArray();

        if (matchingHandlers.Length == 0)
        {
            logger.LogWarning(
                "No handler found. EventId={EventId}, Type={EventType}",
                eventId, eventType);

            return new EventProcessingClaimResult(EventProcessingClaimStatus.NoHandler, eventId, null, null);
        }

        var claim = await eventStore.TryClaimAsync(eventId, cancellationToken);

        if (claim.Status != EventProcessingClaimStatus.Claimed)
        {
            logger.LogDebug(
                "Event processing skipped. EventId={EventId}, Type={EventType}, " +
                "Status={Status}, ClaimedUntilUtc={ClaimedUntilUtc}",
                eventId, eventType, claim.Status, claim.ClaimedUntilUtc);

            return claim;
        }

        var claimToken = claim.ClaimToken
            ?? throw new InvalidOperationException(
                $"Claim token is missing for event {eventId}.");

        logger.LogDebug(
            "Dispatching event. EventId={EventId}, Type={EventType}, HandlerCount={HandlerCount}",
            eventId, eventType, matchingHandlers.Length);

        try
        {
            foreach (var handler in matchingHandlers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                logger.LogDebug(
                    "Executing handler {HandlerType}. EventId={EventId}, Type={EventType}",
                    handler.GetType().Name, eventId, eventType);

                await handler.HandleAsync(domainEvent, cancellationToken);

                logger.LogDebug(
                    "Handler {HandlerType} completed. EventId={EventId}, Type={EventType}",
                    handler.GetType().Name, eventId, eventType);
            }

            await eventStore.MarkAsProcessedAsync(eventId, claimToken, cancellationToken);

            logger.LogInformation(
                "Event processed successfully. EventId={EventId}, Type={EventType}",
                eventId, eventType);

            return claim;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Не пытаемся освобождать claim отменённым токеном.
            // Если claim останется активным, он истечёт по ClaimedUntilUtc.
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Event processing failed. EventId={EventId}, Type={EventType}",
                eventId, eventType);

            metrics.EventFailed();

            var failed = await eventStore.ReleaseClaimAsync(
                eventId,
                claimToken,
                ex.ToString(),
                cancellationToken);

            return new EventProcessingClaimResult(
                failed
                    ? EventProcessingClaimStatus.Failed
                    : EventProcessingClaimStatus.Retry,
                eventId,
                null,
                null);
        }
    }
}