using DDD.Domain.Common.ValueObjects;
using DDD.Domain.Entities;
using DDD.Domain.Enums;
using DDD.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDD.Application.Orders;

public record GetOrdersQuery([FromQuery(Name = "statusId")] int? StatusId);
public record OrderResponse(int Id, string CustomerName, string Email, MoneyDto Price, string Status);

public sealed class EmailAlreadyUsedException(Email email) :
    Exception($"Email '{email.Value}' уже используется.") {}

public interface IOrderService
{
    Task<OrderResponse?> GetOrderByIdAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<OrderResponse>> GetAllOrdersAsync(GetOrdersQuery query, CancellationToken cancellationToken); 
    Task<OrderResponse> CreateOrderAsync(CreateOrderDto dto, CancellationToken cancellationToken);
    Task<OrderResponse?> UpdateOrderAsync(int id, UpdateOrderDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteOrderAsync(int id, CancellationToken cancellationToken);
}

public class OrderService(AppDbContext context) : IOrderService
{
    public async Task<OrderResponse?> GetOrderByIdAsync(int id, CancellationToken cancellationToken)
    {
        var order = await context.Orders.FindAsync([id], cancellationToken);
        return order is null ? null : MapToResponseDto(order);
    }

    public async Task<IEnumerable<OrderResponse>> GetAllOrdersAsync(GetOrdersQuery query, CancellationToken cancellationToken)
    {
        var dbQuery = context.Orders.AsNoTracking();

        if (query.StatusId.HasValue)
        {
            var targetStatus = OrderStatus.FromValue(query.StatusId.Value);
            dbQuery = dbQuery.Where(o => o.Status == targetStatus);
        }

        var orders = await dbQuery.ToListAsync(cancellationToken);
        return orders.Select(MapToResponseDto);
    }
    private async Task<Email> EnsureEmailIsUniqueAsync(
        string emailValue,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var email = Email.Create(emailValue);

        var emailExists = await context.Orders
            .AnyAsync(x => x.Email == email && (excludeId == null || x.Id != excludeId), cancellationToken);
        if (emailExists)
            throw new EmailAlreadyUsedException(email);

        return email;
    }
    public async Task<OrderResponse> CreateOrderAsync(CreateOrderDto dto, CancellationToken cancellationToken)
    {
        var customerName = new CustomerName(dto.CustomerName);
        var email = await EnsureEmailIsUniqueAsync(dto.Email, cancellationToken: cancellationToken);
        var price = new Money(dto.Price.Amount, dto.Price.Currency);

        var order = new Order(customerName, email, price);

        context.Orders.Add(order);
        await context.SaveChangesAsync(cancellationToken);

        return MapToResponseDto(order);
    }
    public async Task<OrderResponse?> UpdateOrderAsync(int id, UpdateOrderDto dto, CancellationToken cancellationToken)
    {
        var order = await context.Orders.FindAsync([id], cancellationToken);
        if (order is null) return null;

        if (dto.CustomerName is not null)
            order.RenameCustomer(dto.CustomerName);

        if (dto.Email is not null)
            order.ChangeEmail(await EnsureEmailIsUniqueAsync(dto.Email, id, cancellationToken));

        if (dto.Price is not null && dto.Price.Amount is not null)
            if (dto.Price.Currency is not null)
                order.ChangePrice(new Money(dto.Price.Amount.Value, dto.Price.Currency));
            else 
                order.ChangePriceAmount(dto.Price.Amount.Value);

        await context.SaveChangesAsync(cancellationToken);

        return MapToResponseDto(order);
    }

    public async Task<bool> StartProcessingAsync(int id, CancellationToken cancellationToken)
    {
        var order = await context.Orders.FindAsync([id], cancellationToken);

        if (order is null)
            return false;

        order.StartProcessing();

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
    public async Task<bool> ShipAsync(int id, CancellationToken cancellationToken)
    {
        var order = await context.Orders.FindAsync([id], cancellationToken);

        if (order is null)
            return false;

        order.Ship();

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
    public async Task<bool> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var order = await context.Orders.FindAsync([id], cancellationToken);

        if (order is null)
            return false;

        order.Cancel();

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
    public async Task<bool> DeleteOrderAsync(int id, CancellationToken cancellationToken)
    {
        var order = await context.Orders.FindAsync([id], cancellationToken);
        if (order is null) return false;

        context.Orders.Remove(order);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static OrderResponse MapToResponseDto(Order order) =>
        new(
            order.Id, 
            order.CustomerName.Value, 
            order.Email.Value, 
            new MoneyDto(order.Price.Amount, order.Price.Currency), 
            order.Status.ToString());
}
