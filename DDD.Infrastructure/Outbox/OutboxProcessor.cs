using Microsoft.EntityFrameworkCore;
using DDD.Eventing.Contracts;

namespace DDD.Infrastructure.Outbox;

public class OutboxProcessor(
    AppDbContext context,
    IOutboxMessageSerializer serializer,
    IDomainEventDispatcher dispatcher)
{
    private const int MaxRetryCount = 5;

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var claimToken = Guid.NewGuid();

        var messages = await ClaimMessagesAsync(claimToken, cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var domainEvent = serializer.Deserialize(message);

                await dispatcher.DispatchAsync(domainEvent, cancellationToken);

                message.MarkAsProcessed(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                var retryCount = message.RetryCount + 1;

                if (retryCount >= MaxRetryCount)
                {
                    message.MarkAsFailed(ex.Message);
                }
                else
                {
                    var nextAttempt = DateTime.UtcNow
                        .Add(GetRetryDelay(retryCount));

                    message.ScheduleRetry(ex.Message, nextAttempt);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<OutboxMessage>> ClaimMessagesAsync(Guid claimToken, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var claimedUntil = now.AddMinutes(1);

        var candidates = await context.OutboxMessages
            .Where(x =>
                x.ProcessedOnUtc == null &&
                x.FailedOnUtc == null &&
                (x.NextAttemptOnUtc == null || x.NextAttemptOnUtc <= now) &&
                (x.ClaimedUntilUtc == null || x.ClaimedUntilUtc < now))
            .OrderBy(x => x.Id)
            .Take(20)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in candidates)
        {
            await context.OutboxMessages
                .Where(x =>
                    x.Id == id &&
                    x.ProcessedOnUtc == null &&
                    x.FailedOnUtc == null &&
                    (x.NextAttemptOnUtc == null || x.NextAttemptOnUtc <= now) &&
                    (x.ClaimedUntilUtc == null || x.ClaimedUntilUtc < now))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.ClaimToken, claimToken)
                        .SetProperty(x => x.ClaimedUntilUtc, claimedUntil),
                    cancellationToken);
        }

        return await context.OutboxMessages
            .Where(x =>
                x.ClaimToken == claimToken &&
                x.ProcessedOnUtc == null)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }
    private static TimeSpan GetRetryDelay(int retryCount)
    {
        var seconds = Math.Min(Math.Pow(2, retryCount), 300);

        return TimeSpan.FromSeconds(60);
//        return TimeSpan.FromSeconds(seconds);

    }
}