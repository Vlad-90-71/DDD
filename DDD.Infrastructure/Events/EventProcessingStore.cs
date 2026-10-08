using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DDD.Eventing.Contracts;

namespace DDD.Infrastructure.Events;

public sealed class EventProcessingStore(AppDbContext context, ILogger<EventProcessingStore> logger)
    : IEventProcessingStore
{
    private const int MaxAttemptCount = 3;
    private static readonly TimeSpan ClaimDuration = TimeSpan.FromSeconds(30);

    public async Task<EventProcessingClaimResult> TryClaimAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var claimToken = Guid.NewGuid();
        var claimedUntil = now.Add(ClaimDuration);

        var updated = await context.EventProcessingStates
            .Where(x =>
                x.EventId == eventId &&
                x.ProcessedOnUtc == null && x.FailedOnUtc == null &&
                (x.ClaimToken == null || x.ClaimedUntilUtc <= now))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ClaimToken, claimToken)
                    .SetProperty(x => x.ClaimedUntilUtc, claimedUntil)
                    .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1)
                    .SetProperty(x => x.UpdatedOnUtc, now),
                cancellationToken);

        if (updated == 1)
        {
            logger.LogDebug(
                "Event claim acquired. EventId={EventId}, ClaimToken={ClaimToken}, ClaimedUntil={ClaimedUntil}",
                eventId, claimToken, claimedUntil);

            return new EventProcessingClaimResult(EventProcessingClaimStatus.Claimed, eventId, claimToken);
        }

        var existing = await context.EventProcessingStates
            .AsNoTracking()
            .SingleAsync(x => x.EventId == eventId, cancellationToken);

        var status = existing switch
        {
            { ProcessedOnUtc: not null } => EventProcessingClaimStatus.AlreadyProcessed,
            { FailedOnUtc: not null } => EventProcessingClaimStatus.Failed,
            _ => EventProcessingClaimStatus.InProgress
        };

        logger.LogDebug(
            "Event claim not acquired. EventId={EventId}, Status={Status}, AttemptCount={AttemptCount}",
            eventId, status, existing.AttemptCount);

        return new EventProcessingClaimResult(status, eventId, null);
    }

    public async Task MarkAsProcessedAsync(Guid eventId, Guid claimToken, CancellationToken cancellationToken)
    {
        var processedEvent = await context.EventProcessingStates
            .SingleAsync(x => x.EventId == eventId, cancellationToken);

        processedEvent.MarkAsProcessed(claimToken, DateTime.UtcNow);

        logger.LogDebug(
            "Event marked as processed. EventId={EventId}, ClaimToken={ClaimToken}",
            eventId, claimToken);
    }

    public async Task<bool> ReleaseClaimAsync(Guid eventId, Guid claimToken, string error, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var eventEntry = await context.EventProcessingStates
            .AsNoTracking()
            .SingleAsync(x => x.EventId == eventId, cancellationToken);

        if (eventEntry.ClaimToken != claimToken)
        {
            logger.LogWarning(
                "Cannot release event claim because claim token does not match. EventId={EventId}, ClaimToken={ClaimToken}",
                eventId, claimToken);

            return false;
        }

        if (eventEntry.AttemptCount >= MaxAttemptCount)
        {
            await context.EventProcessingStates
                .Where(x =>
                    x.EventId == eventId &&
                    x.ClaimToken == claimToken)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.ClaimToken, (Guid?)null)
                        .SetProperty(x => x.ClaimedUntilUtc, (DateTime?)null)
                        .SetProperty(x => x.FailedOnUtc, now)
                        .SetProperty(x => x.LastError, error)
                        .SetProperty(x => x.UpdatedOnUtc, now),
                    cancellationToken);

            logger.LogError(
                "Event processing permanently failed. EventId={EventId}, AttemptCount={AttemptCount}",
                eventId, eventEntry.AttemptCount);

            return true;
        }

        await context.EventProcessingStates
            .Where(x =>
                x.EventId == eventId &&
                x.ClaimToken == claimToken)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ClaimToken, (Guid?)null)
                    .SetProperty(x => x.ClaimedUntilUtc, (DateTime?)null)
                    .SetProperty(x => x.LastError, error)
                    .SetProperty(x => x.UpdatedOnUtc, now),
                cancellationToken);

        logger.LogWarning(
            "Event claim released for retry. EventId={EventId}, AttemptCount={AttemptCount}",
            eventId, eventEntry.AttemptCount);

        return false;
    }
}