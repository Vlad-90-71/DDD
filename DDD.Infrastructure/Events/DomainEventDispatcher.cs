using DDD.Domain.Common.Events;
using DDD.Eventing.Contracts;

namespace DDD.Infrastructure.Events;

public class DomainEventDispatcher(IEnumerable<IDomainEventHandler> handlers) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();

        foreach (var handler in handlers)
        {
            if (handler.EventType != eventType)
                continue;

            await handler.HandleAsync(domainEvent, cancellationToken);
        }
    }
}