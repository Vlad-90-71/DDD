using DDD.Domain.Common.Events;
using DDD.Domain.Entities.Order;
using DDD.Eventing.Contracts;

namespace DDD.Application.Services.OrderService;

public sealed class OrderProcessingRequestedEventHandler(IOrderRepository orders, IEventProcessingStore eventStore)
    : OrderRequestEventHandler<OrderProcessingRequestedEvent>(orders, eventStore)
{
    protected override Task HandleEventAsync(OrderProcessingRequestedEvent domainEvent, CancellationToken cancellationToken) =>
        OrderHandleAsync(domainEvent.OrderId, order => order.Process(), cancellationToken);
}

public abstract class OrderRequestEventHandler<TEvent>(IOrderRepository orders, IEventProcessingStore eventStore)
    : IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    public async Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken)
    {
        if (await eventStore.IsProcessedAsync(domainEvent.EventId, cancellationToken))
            return;

        await HandleEventAsync(domainEvent, cancellationToken);

        eventStore.LogProcessed(domainEvent);

        eventStore.MarkAsProcessed(domainEvent.EventId);
    }

    protected abstract Task HandleEventAsync(TEvent domainEvent, CancellationToken cancellationToken);

    protected async Task OrderHandleAsync(int orderId, Action<Order> action, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Заказ с Id '{orderId}' не найден.");

        action(order);
    }
}