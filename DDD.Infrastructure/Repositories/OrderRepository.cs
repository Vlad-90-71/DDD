using Microsoft.EntityFrameworkCore;
using DDD.Domain.Common.ValueObjects;
using DDD.Domain.Entities.Order;
using DDD.Application.Services.OrderService;

namespace DDD.Infrastructure.Repositories;

public class OrderRepository(AppDbContext context) : Repository<Order, int>(context), IOrderRepository
{
    private readonly AppDbContext _context = context;
    public async Task ReloadAsync(int id, CancellationToken cancellationToken)
    {
        var order = _context.Orders.Local.FirstOrDefault(x => x.Id == id);

        if (order is not null)
            await _context.Entry(order).ReloadAsync(cancellationToken);
    }
    public Task<Order?> GetByIdReadOnlyAsync(int id, CancellationToken cancellationToken) =>
        _context.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetAllAsync(
        OrderStatus? status, CancellationToken cancellationToken)
    {
        var query = _context.Orders.AsNoTracking();

        if (status is not null)
            query = query.Where(x => x.Status == status);

        return await query.ToListAsync(cancellationToken);
    }

    public Task<bool> EmailExistsAsync(Email email, int? excludeId, CancellationToken cancellationToken) =>
        _context.Orders.AnyAsync(
            x => x.Email == email && (excludeId == null || x.Id != excludeId), cancellationToken);
}