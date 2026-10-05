using DDD.Domain.Common.Events;
using DDD.Domain.Entities.Order;
using DDD.Eventing.Contracts;
using System.Collections.Concurrent;

namespace DDD.Application.Services.OrderService;

public sealed class OrderProcessingRequestedEventHandler(IOrderRepository orders)
    : OrderRequestEventHandler<OrderProcessingRequestedEvent>(orders)
{
    private static readonly ConcurrentDictionary<Guid, int> Attempts = [];
    protected override async Task HandleEventAsync(OrderProcessingRequestedEvent domainEvent, CancellationToken cancellationToken)
    {
        await OrderHandleAsync(domainEvent.OrderId, order => order.Process(), cancellationToken);
        
        var attempt = Attempts.AddOrUpdate(domainEvent.EventId, 1, (_, value) => value + 1);

        Console.WriteLine($"Handler: EventId={domainEvent.EventId}, Attempt={attempt}");

        if (attempt == 1)
            throw new InvalidOperationException("TEST: ошибка первой попытки.");
    }
}

public abstract class OrderRequestEventHandler<TEvent>(IOrderRepository orders) 
    : IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken) =>
        HandleEventAsync(domainEvent, cancellationToken);

    protected abstract Task HandleEventAsync(TEvent domainEvent, CancellationToken cancellationToken);

    protected async Task OrderHandleAsync(int orderId, Action<Order> action, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Заказ с Id '{orderId}' не найден.");

        action(order);
    }
}