using DDD.Domain.Common.Events;

namespace DDD.Eventing.Contracts;

public interface IDomainEventDispatcher
{
    Task<EventProcessingClaimResult> DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}