namespace DDD.Application.Services.OrderService;

public interface IOrderService
{
    Task<OrderResponse?> GetOrderByIdAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<OrderResponse>> GetAllOrdersAsync(GetOrdersQuery query, CancellationToken cancellationToken);
    Task<OrderResponse> CreateOrderAsync(CreateOrderDto dto, CancellationToken cancellationToken);
    Task<OrderResponse?> UpdateOrderAsync(int id, UpdateOrderDto dto, CancellationToken cancellationToken);
    Task<bool> StartProcessingAsync(int id, string address, CancellationToken cancellationToken);
    Task<bool> ShipAsync(int id, CancellationToken cancellationToken);
    Task<bool> CancelAsync(int id, CancellationToken cancellationToken);
    Task<bool> DeleteOrderAsync(int id, CancellationToken cancellationToken);
    Task Test(CancellationToken cancellationToken);
}
