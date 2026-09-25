namespace DDD.Eventing.Contracts;

public interface IEventProcessingStore
{
    Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken);
    void Log(Guid eventId, string eventType, string message);
    void MarkAsProcessed( Guid eventId);
}