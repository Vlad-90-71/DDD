using DDD.Domain.Entities;
using DDD.Application.Common.Events;

namespace DDD.Application.Orders.Events;

public class OrderProcessingStartedEventHandler : IDomainEventHandler<OrderProcessingStartedEvent>
{
    public Task HandleAsync(OrderProcessingStartedEvent domainEvent, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Заказ {domainEvent.OrderId} переведен в обработку.");

        return Task.CompletedTask;
    }
}