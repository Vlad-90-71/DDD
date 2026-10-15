using DDD.Domain.Common.Events;
using DDD.Domain.Entities.Order;

namespace DDD.Application.Services.OrderService;

public abstract class OrderEventHandler<TEvent>(IOrderRepository orderRepository,
    Func<TEvent, (int Id, Action<Order> Action)> mapper) 
    : EntityEventHandler<Order, int, TEvent>(orderRepository, mapper)
    where TEvent : DomainEvent { }

/*public sealed class OrderProcessingRequestedEventHandler(IOrderRepository orderRepository)
    : EntityEventHandler<Order, int, OrderProcessingRequestedEvent>(
        orderRepository, e => (e.OrderId, order => order.Process())) { }
*/

public sealed class OrderProcessingRequestedEventHandler(IOrderRepository orderRepository)
    : OrderEventHandler<OrderProcessingRequestedEvent>(orderRepository, e => (e.OrderId, order => order.Process())) { }

public sealed class OrderShippedEventHandler(IOrderService orderService)
    : DomainEventHandler<OrderShippedEvent>((e, ct) => orderService.HandleOrderShippedAsync(e, ct)) 
{
    public override TimeSpan? ClaimedUntilUtc { get; } =  TimeSpan.FromSeconds(40);
}
