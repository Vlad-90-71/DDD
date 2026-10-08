using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DDD.MessageBroker.Contracts;

namespace DDD.Infrastructure.Outbox;

public class OutboxProcessor(AppDbContext context, IMessagePublisher publisher, ILogger<OutboxProcessor> logger)
{
    private const int MaxRetryCount = 5;
    private const int MaxRetryDelaySeconds = 300;
    private const int BatchSize = 20;

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var claimToken = Guid.NewGuid();

        var messages = await ClaimMessagesAsync(claimToken, cancellationToken);

        if (messages.Length == 0)
            return;

        logger.LogDebug(
            "Claimed {MessageCount} Outbox message(s). ClaimToken={ClaimToken}",
            messages.Length, claimToken);

        foreach (var message in messages)
        {
            try
            {
                logger.LogDebug(
                    "Publishing Outbox message. OutboxMessageId={OutboxMessageId}, EventId={EventId}, Type={EventType}",
                    message.Id, message.EventId, message.Type);

                var brokerMessage = new BrokerMessage(message.EventId, message.Type,  message.Content);
                await publisher.PublishAsync(brokerMessage, cancellationToken);

                message.MarkAsProcessed(DateTime.UtcNow);

                logger.LogDebug(
                    "Outbox message published successfully. OutboxMessageId={OutboxMessageId}, EventId={EventId}, Type={EventType}",
                    message.Id, message.EventId, message.Type);
            }
            catch (Exception ex)
            {
                var retryCount = message.RetryCount + 1;

                if (retryCount <= MaxRetryCount)
                {
                    var nextAttempt = DateTime.UtcNow.Add(GetRetryDelay(retryCount));
                    message.ScheduleRetry(ex.Message, nextAttempt);

                    logger.LogWarning(ex,
                        "Outbox message publishing failed. Retry scheduled. " +
                        "OutboxMessageId={OutboxMessageId}, " +
                        "EventId={EventId}, RetryCount={RetryCount}, " +
                        "NextAttemptOnUtc={NextAttemptOnUtc}",
                        message.Id, message.EventId, retryCount, nextAttempt);
                }
                else
                {
                    message.MarkAsFailed(ex.Message);

                    logger.LogError(ex,
                        "Outbox message permanently failed. OutboxMessageId={OutboxMessageId}, EventId={EventId}, RetryCount={RetryCount}",
                        message.Id, message.EventId, retryCount);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogDebug(
            "Outbox processing completed. MessageCount={MessageCount}, ClaimToken={ClaimToken}",
            messages.Length, claimToken);
    }

    private async Task<OutboxMessage[]> ClaimMessagesAsync(Guid claimToken, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var claimedUntil = now.AddMinutes(1);

        var candidates = await context.OutboxMessages
            .Where(x =>
                x.ProcessedOnUtc == null &&  x.FailedOnUtc == null &&
                (x.NextAttemptOnUtc == null || x.NextAttemptOnUtc <= now) &&
                (x.ClaimedUntilUtc == null || x.ClaimedUntilUtc < now))
            .OrderBy(x => x.Id)
            .Take(BatchSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
            return [];

        var claimedCount = await context.OutboxMessages
            .Where(x =>
                candidates.Contains(x.Id) &&
                x.ProcessedOnUtc == null && x.FailedOnUtc == null &&
                (x.NextAttemptOnUtc == null || x.NextAttemptOnUtc <= now) &&
                (x.ClaimedUntilUtc == null || x.ClaimedUntilUtc < now))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ClaimToken, claimToken)
                    .SetProperty(x => x.ClaimedUntilUtc, claimedUntil),
                cancellationToken);

        if (claimedCount == 0)
            return [];

        return await context.OutboxMessages
            .Where(x =>
                x.ClaimToken == claimToken &&
                x.ProcessedOnUtc == null)
            .OrderBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
    }

    private static TimeSpan GetRetryDelay(int retryCount)
    {
        var seconds = Math.Min(Math.Pow(2, retryCount), MaxRetryDelaySeconds);

        return TimeSpan.FromSeconds(seconds);
    }
}