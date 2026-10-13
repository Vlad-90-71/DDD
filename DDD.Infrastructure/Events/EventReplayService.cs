using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DDD.Eventing.Contracts;

namespace DDD.Infrastructure.Events;

public sealed class EventReplayService(AppDbContext context, ILogger<EventReplayService> logger)
    : IEventReplayService
{
    public async Task<bool> ReplayAsync(Guid eventId, CancellationToken cancellationToken)
    {
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var message = await context.OutboxMessages
                .SingleOrDefaultAsync(x => x.EventId == eventId && x.ProcessedOnUtc != null, cancellationToken);

            if (message is null)
            {
                await transaction.RollbackAsync(cancellationToken);

                logger.LogWarning(
                    "Replay rejected: published Outbox message not found. EventId={EventId}",
                    eventId);

                return false;
            }

            var now = DateTime.UtcNow;

            var updated = await context.EventProcessingStates
                .Where(x => x.EventId == eventId && x.FailedOnUtc != null && x.ProcessedOnUtc == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.AttemptCount, 0)
                    .SetProperty(x => x.ClaimToken, (Guid?)null)
                    .SetProperty(x => x.ClaimedUntilUtc, (DateTime?)null)
                    .SetProperty(x => x.FailedOnUtc, (DateTime?)null)
                    .SetProperty(x => x.LastError, (string?)null)
                    .SetProperty(x => x.UpdatedOnUtc, now),
                    cancellationToken);

            if (updated != 1)
            {
                await transaction.RollbackAsync(cancellationToken);

                logger.LogWarning(
                    "Replay rejected: event is not failed or has already been replayed. EventId={EventId}",
                    eventId);

                return false;
            }

            // Повторно используем существующую запись Outbox.
            message.ScheduleReplay();

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Event replay scheduled through existing Outbox message. EventId={EventId}, OutboxMessageId={OutboxMessageId}",
                eventId, message.Id);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}