using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DDD.Domain.Enums;
using DDD.Domain.Entities;
using DDD.Infrastructure;

namespace DDD.Application.Orders;

public class CreateOrderDto(string customerName, OrderStatus status)
{
    public string CustomerName { get; set; } = customerName;
    public OrderStatus Status { get; set; } = status;
}
public class UpdateOrderDto(string? customerName = null, OrderStatus? status = null)
{
    public string? CustomerName { get; set; } = customerName;
    public OrderStatus? Status { get; set; } = status;
}

public record GetOrdersQuery([FromQuery(Name = "statusId")] int? StatusId);
public record OrderResponse(int Id, string CustomerName,int StatusId, string StatusName);

public interface IOrderService
{
    Task<OrderResponse?> GetOrderByIdAsync(int id);
    Task<IEnumerable<OrderResponse>> GetAllOrdersAsync(GetOrdersQuery query); 
    Task<OrderResponse> CreateOrderAsync(CreateOrderDto dto);
    Task<OrderResponse?> UpdateOrderAsync(int id, UpdateOrderDto dto);
    Task<bool> DeleteOrderAsync(int id);
}

public class OrderService(AppDbContext context) : IOrderService
{
    public async Task<OrderResponse?> GetOrderByIdAsync(int id)
    {
        var order = await context.Orders.FindAsync(id);
        return order is null ? null : MapToResponseDto(order);
    }

    public async Task<IEnumerable<OrderResponse>> GetAllOrdersAsync(GetOrdersQuery query)
    {
        var dbQuery = context.Orders.AsNoTracking();

        if (query.StatusId.HasValue)
        {
            var targetStatus = OrderStatus.FromValue(query.StatusId.Value);
            dbQuery = dbQuery.Where(o => o.Status == targetStatus);
        }

        var orders = await dbQuery.ToListAsync();
        return orders.Select(MapToResponseDto);
    }

    public async Task<OrderResponse> CreateOrderAsync(CreateOrderDto dto)
    {
        var order = new Order(dto.CustomerName, dto.Status);

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        return MapToResponseDto(order);
    }

    public async Task<OrderResponse?> UpdateOrderAsync(int id, UpdateOrderDto dto)
    {
        var order = await context.Orders.FindAsync(id);
        if (order is null) return null;

        if (dto.CustomerName is not null)
            order.UpdateCustomerName(dto.CustomerName);

        if (dto.Status is not null)
            order.UpdateStatus(dto.Status);

        await context.SaveChangesAsync();

        return MapToResponseDto(order);
    }

    public async Task<bool> DeleteOrderAsync(int id)
    {
        var order = await context.Orders.FindAsync(id);
        if (order is null) return false;

        context.Orders.Remove(order);
        await context.SaveChangesAsync();

        return true;
    }

    private static OrderResponse MapToResponseDto(Order order) =>
        new(order.Id, order.CustomerName, order.Status.Value, order.Status.ToString());
}
