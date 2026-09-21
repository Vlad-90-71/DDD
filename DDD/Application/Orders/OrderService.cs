using DDD.Domain.Entities;
using DDD.Domain.Enums;
using DDD.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDD.Application.Orders;

//public record OrderDto(string CustomerName, OrderStatus Status);
public record OrderDto
{
    public string CustomerName { get; init; } = string.Empty;
    public OrderStatus Status { get; init; } = null!;
}

public record GetOrdersQuery([FromQuery(Name = "statusId")] int? StatusId);
public record OrderResponse(int Id, string CustomerName,int StatusId ,string StatusName);

public interface IOrderService
{
    Task<OrderResponse?> GetOrderByIdAsync(int id);
    Task<IEnumerable<OrderResponse>> GetAllOrdersAsync(GetOrdersQuery query); 
    Task<OrderResponse> CreateOrderAsync(OrderDto dto);
    Task<OrderResponse?> UpdateOrderAsync(int id, OrderDto dto);
    Task<bool> DeleteOrderAsync(int id);
}

// Из конструктора убраны ВСЕ IValidator<T>, так как валидация уже произошла в фильтре контроллера
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

    public async Task<OrderResponse> CreateOrderAsync(OrderDto dto)
    {
        var order = new Order(dto.CustomerName, dto.Status);

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        return MapToResponseDto(order);
    }

    public async Task<OrderResponse?> UpdateOrderAsync(int id, OrderDto dto)
    {
        var order = await context.Orders.FindAsync(id);
        if (order is null) return null;

        order.UpdateCustomerName(dto.CustomerName);
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
