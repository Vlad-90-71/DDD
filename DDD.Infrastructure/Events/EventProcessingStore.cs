using DDD.Domain.Common.Events;
using DDD.Eventing.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DDD.Infrastructure.Events;

public sealed class EventProcessingStore(AppDbContext context) : IEventProcessingStore
{
    public Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken) =>
        context.ProcessedEvents.AnyAsync(x => x.EventId == eventId, cancellationToken);

    public void LogProcessed(IDomainEvent domainEvent)
    {
        context.EventLogs.Add(new EventLog(
            domainEvent.EventId,
            domainEvent.GetType().Name,
            $"Consumer обработал событие '{domainEvent.GetType().Name}'.",
            DateTime.UtcNow));
    }

    public void MarkAsProcessed(Guid eventId)
    {
        context.ProcessedEvents.Add(new ProcessedEvent(eventId, DateTime.UtcNow));
    }
}