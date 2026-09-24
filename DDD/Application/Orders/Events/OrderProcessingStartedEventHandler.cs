using Microsoft.EntityFrameworkCore;
using DDD.Domain.Entities;
using DDD.Infrastructure;
using DDD.Infrastructure.Outbox;
using DDD.Application.Common.Events;
using DDD.Infrastructure.Log;

namespace DDD.Application.Orders.Events;

public class OrderProcessingStartedEventHandler(AppDbContext context) 
    : IDomainEventHandler<OrderProcessingStartedEvent>
{
    public async Task HandleAsync(OrderProcessingStartedEvent domainEvent, CancellationToken cancellationToken)
    {
        var alreadyProcessed = await context.ProcessedEvents
            .AnyAsync(x => x.EventId == domainEvent.EventId, cancellationToken);

        if (alreadyProcessed)
            return;

        context.EventLogs.Add(
            new EventLog(
                domainEvent.EventId,
                nameof(OrderProcessingStartedEvent),
                $"Заказ {domainEvent.OrderId} переведен в обработку.",
                DateTime.UtcNow));

        context.ProcessedEvents.Add(
            new ProcessedEvent(domainEvent.EventId, DateTime.UtcNow));
    }
}