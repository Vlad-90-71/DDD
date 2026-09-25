using DDD.Domain.Common.Events;

namespace DDD.Eventing.Contracts;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}

