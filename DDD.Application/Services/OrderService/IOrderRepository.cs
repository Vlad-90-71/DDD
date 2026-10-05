using DDD.Domain.Enums;
using DDD.Domain.Common.ValueObjects;
using DDD.Domain.Entities.Order;

namespace DDD.Application.Services.OrderService;

public interface IOrderRepository
{
    Task ReloadAsync(int id, CancellationToken cancellationToken);
    ValueTask<Order?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Order?> GetByIdReadOnlyAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetAllAsync(OrderStatus? status, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(Email email, int? excludeId, CancellationToken cancellationToken);
    void Add(Order order);
    void Remove(Order order);
}