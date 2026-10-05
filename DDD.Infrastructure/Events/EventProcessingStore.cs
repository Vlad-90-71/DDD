using Microsoft.EntityFrameworkCore;
using DDD.Domain.Common.Events;
using DDD.Eventing.Contracts;

namespace DDD.Infrastructure.Events;

public sealed class EventProcessingStore(AppDbContext context) : IEventProcessingStore
{
    private const int MaxAttemptCount = 3;
    private static readonly TimeSpan ClaimDuration = TimeSpan.FromSeconds(30);

    public async Task<EventProcessingClaimResult> TryClaimAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var claimToken = Guid.NewGuid();
        var claimedUntil = now.Add(ClaimDuration);

        var updated = await context.ProcessedEvents
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
            return new EventProcessingClaimResult(EventProcessingClaimStatus.Claimed, eventId, claimToken);

        var existing = await context.ProcessedEvents.AsNoTracking()
            .SingleAsync(x => x.EventId == eventId, cancellationToken);

        var status = existing switch
        {
            { ProcessedOnUtc: not null } =>  EventProcessingClaimStatus.AlreadyProcessed,
            { FailedOnUtc: not null } => EventProcessingClaimStatus.Failed,
            _ => EventProcessingClaimStatus.InProgress
        };

        return new EventProcessingClaimResult(status, eventId, null);
    }

    public async Task MarkAsProcessedAsync(Guid eventId, Guid claimToken, CancellationToken cancellationToken)
    {
        var processedEvent = await context.ProcessedEvents
            .SingleAsync(x => x.EventId == eventId, cancellationToken);

        processedEvent.MarkAsProcessed(claimToken, DateTime.UtcNow);
    }

    public async Task<bool> ReleaseClaimAsync(Guid eventId, Guid claimToken, string error, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var eventEntry = await context.ProcessedEvents.AsNoTracking()
            .SingleAsync(x => x.EventId == eventId, cancellationToken);

        if (eventEntry.ClaimToken != claimToken)
            return false;

        if (eventEntry.AttemptCount >= MaxAttemptCount)
        {
            await context.ProcessedEvents
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

            return true;
        }

        await context.ProcessedEvents
            .Where(x =>
                x.EventId == eventId &&
                x.ClaimToken == claimToken)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ClaimToken, (Guid?)null)
                    .SetProperty(  x => x.ClaimedUntilUtc, (DateTime?)null)
                    .SetProperty(x => x.LastError, error)
                    .SetProperty(x => x.UpdatedOnUtc, now),
                cancellationToken);

        return false;
    }

    public void LogProcessed(IDomainEvent domainEvent)
    {
        context.EventLogs.Add(
            new EventLog(
                domainEvent.EventId,
                domainEvent.GetType().Name,
                $"Consumer обработал событие '{domainEvent.GetType().Name}'.",
                DateTime.UtcNow));
    }
}