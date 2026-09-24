using Microsoft.EntityFrameworkCore;
using DDD.Domain.Entities;
using DDD.Infrastructure;
using DDD.Infrastructure.Outbox;
using DDD.Application.Common.Events;
using DDD.Infrastructure.Log;

namespace DDD.Application.Orders.Events;

public class OrderCanceledEventHandler(AppDbContext context) 
    : IDomainEventHandler<OrderCanceledEvent>
{
    public async Task HandleAsync(OrderCanceledEvent domainEvent, CancellationToken cancellationToken)
    {
        var alreadyProcessed = await context.ProcessedEvents
            .AnyAsync(x => x.EventId == domainEvent.EventId, cancellationToken);

        if (alreadyProcessed)
            return;

        context.EventLogs.Add(
            new EventLog(
                domainEvent.EventId,
                nameof(OrderCanceledEvent),
                $"Заказ {domainEvent.OrderId} отменен.",
                DateTime.UtcNow));

        context.ProcessedEvents.Add(
            new ProcessedEvent(domainEvent.EventId, DateTime.UtcNow));
    }
}