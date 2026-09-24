using DDD.Application.Common;
using DDD.Application.Common.Events;
using DDD.Domain.Entities;
using DDD.Infrastructure;
using DDD.Infrastructure.Log;
using DDD.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace DDD.Application.Orders.Events;

public class OrderShippedEventHandler(AppDbContext context, IFailureSimulator failureSimulator) 
    : IDomainEventHandler<OrderShippedEvent>
{
    public async Task HandleAsync(OrderShippedEvent domainEvent, CancellationToken cancellationToken)
    {
        var alreadyProcessed = await context.ProcessedEvents
            .AnyAsync(x => x.EventId == domainEvent.EventId,cancellationToken);

        if (alreadyProcessed)
        {
            context.EventLogs.Add(
                new EventLog(
                    domainEvent.EventId,
                    nameof(OrderShippedEvent),
                    $"Повторная доставка. Заказ {domainEvent.OrderId} уже обработан.",
                    DateTime.UtcNow));

            return;
        }

        context.EventLogs.Add(
            new EventLog(
                domainEvent.EventId,
                nameof(OrderShippedEvent),
                $"Заказ {domainEvent.OrderId} был доставлен.",
                DateTime.UtcNow));

        if (failureSimulator.Enabled)
        {
            await context.SaveChangesAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Тестовый сбой ПОСЛЕ EventLog. EventId: {domainEvent.EventId}");
        }

        context.ProcessedEvents.Add(
            new ProcessedEvent(domainEvent.EventId, DateTime.UtcNow));
    }
}