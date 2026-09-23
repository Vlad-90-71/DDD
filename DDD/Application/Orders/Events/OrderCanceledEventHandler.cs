using DDD.Domain.Entities;
using DDD.Application.Common.Events;

namespace DDD.Application.Orders.Events;

public class OrderCanceledEventHandler : IDomainEventHandler<OrderCanceledEvent>
{
    public Task HandleAsync(OrderCanceledEvent domainEvent, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Заказ {domainEvent.OrderId} отменен.");

        return Task.CompletedTask;
    }
}