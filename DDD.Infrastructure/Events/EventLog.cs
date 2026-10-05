using DDD.Domain.Common.Events;

namespace DDD.Infrastructure.Events;

public sealed class EventLog
{
    public long Id { get; private set; }
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public DateTime CreatedOnUtc { get; private set; }

    private EventLog() { }

    public EventLog(Guid eventId, string eventType, string message, DateTime createdOnUtc)
    {
        EventId = eventId;
        EventType = eventType;
        Message = message;
        CreatedOnUtc = createdOnUtc;
    }
    public EventLog(IDomainEvent domainEvent)
    {
        EventId = domainEvent.EventId;
        EventType = domainEvent.GetType().Name;
        Message = domainEvent.Message;
        CreatedOnUtc = domainEvent.OccurredOnUtc;
    }
}
