using DDD.Domain.Common.Events;
using DDD.Domain.Entities.Order;

namespace DDD.Application.Services.OrderService;

public sealed class OrderProcessingRequestedEventHandler(IOrderRepository orderRepository)
    : EntityEventHandler<Order, int, OrderProcessingRequestedEvent>(
        orderRepository, e => (e.OrderId, order => order.Process())) { }

public sealed class OrderShippedEventHandler(IOrderService orderService)
    : ServiceEventHandler<OrderShippedEvent>((e, ct) => orderService.HandleOrderShippedAsync(e, ct)) { }