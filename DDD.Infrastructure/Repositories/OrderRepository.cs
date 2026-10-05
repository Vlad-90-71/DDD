using Microsoft.EntityFrameworkCore;
using DDD.Domain.Enums;
using DDD.Domain.Common.ValueObjects;
using DDD.Domain.Entities.Order;
using DDD.Application.Services.OrderService;

namespace DDD.Infrastructure.Repositories;

public sealed class OrderRepository(AppDbContext context) : IOrderRepository
{
    public async Task ReloadAsync(int id, CancellationToken cancellationToken)
    {
        var order = context.Orders.Local.FirstOrDefault(x => x.Id == id);

        if (order is not null)
            await context.Entry(order).ReloadAsync(cancellationToken);
    }

    public ValueTask<Order?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        context.Orders.FindAsync([id], cancellationToken);
    public Task<Order?> GetByIdReadOnlyAsync(int id, CancellationToken cancellationToken) =>
        context.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<IReadOnlyList<Order>> GetAllAsync(
        OrderStatus? status, CancellationToken cancellationToken)
    {
        var query = context.Orders.AsNoTracking();

        if (status is not null)
            query = query.Where(x => x.Status == status);

        return await query.ToListAsync(cancellationToken);
    }

    public Task<bool> EmailExistsAsync(Email email, int? excludeId, CancellationToken cancellationToken) =>
        context.Orders.AnyAsync(
            x => x.Email == email && (excludeId == null || x.Id != excludeId), cancellationToken);

    public void Add(Order order) =>
        context.Orders.Add(order);

    public void Remove(Order order) =>
        context.Orders.Remove(order);
}