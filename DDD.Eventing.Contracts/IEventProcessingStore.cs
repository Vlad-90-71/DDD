using DDD.Domain.Common.Events;

namespace DDD.Eventing.Contracts;

public interface IEventProcessingStore
{
    Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken);
    void LogProcessed(IDomainEvent domainEvent);
    void MarkAsProcessed( Guid eventId);
}