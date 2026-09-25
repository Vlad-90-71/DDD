using Microsoft.EntityFrameworkCore;
using DDD.Eventing.Contracts;

namespace DDD.Infrastructure.Events;

public sealed class EventProcessingStore(AppDbContext context) : IEventProcessingStore
{
    public Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken) =>
        context.ProcessedEvents.AnyAsync(x => x.EventId == eventId, cancellationToken);

    public void Log(Guid eventId, string eventType, string message)
    {
        context.EventLogs.Add(new EventLog(eventId, eventType, message, DateTime.UtcNow));
    }

    public void MarkAsProcessed(Guid eventId) =>
        context.ProcessedEvents.Add(new ProcessedEvent(eventId, DateTime.UtcNow));
}