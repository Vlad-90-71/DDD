using DDD.Domain.Enums;
using DDD.Domain.Common.ValueObjects;
using DDD.Domain.Entities;

namespace DDD.Application.Services;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetAllAsync(OrderStatus? status, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(Email email, int? excludeId, CancellationToken cancellationToken);
    void Add(Order order);
    void Remove(Order order);
}