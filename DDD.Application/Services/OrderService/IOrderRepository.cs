using DDD.Domain.Common.ValueObjects;
using DDD.Domain.Entities.Order;
using DDD.Domain.Common;

namespace DDD.Application.Services.OrderService;

public interface IOrderRepository : IRepository<Order, int>
{
    Task ReloadAsync(int id, CancellationToken cancellationToken);
    Task<Order?> GetByIdReadOnlyAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetAllAsync(OrderStatus? status, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(Email email, int? excludeId, CancellationToken cancellationToken);
}