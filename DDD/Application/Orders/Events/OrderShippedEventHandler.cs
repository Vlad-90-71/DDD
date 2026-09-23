using DDD.Domain.Entities;
using DDD.Application.Common.Events;

namespace DDD.Application.Orders.Events;

public class OrderShippedEventHandler : IDomainEventHandler<OrderShippedEvent>
{
    public Task HandleAsync(OrderShippedEvent domainEvent, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Заказ {domainEvent.OrderId} был доставлен.");

        return Task.CompletedTask;
    }
}