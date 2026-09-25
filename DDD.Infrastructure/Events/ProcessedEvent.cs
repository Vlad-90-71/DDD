namespace DDD.Infrastructure.Events;

public class ProcessedEvent
{
    public Guid EventId { get; private set; }
    public DateTime ProcessedOnUtc { get; private set; }
    
    private ProcessedEvent() { }

    public ProcessedEvent(Guid eventId, DateTime processedOnUtc)
    {
        EventId = eventId;
        ProcessedOnUtc = processedOnUtc;
    }
}
