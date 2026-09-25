using System.Data;
using DDD.Domain.Common.ValueObjects;
using DDD.Domain.Enums;
using DDD.Domain.Entities;
using DDD.Application.Common;
using DDD.Application.Services.Dto;

namespace DDD.Application.Services;

public record GetOrdersQuery(int? StatusId);
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
    Task Test(CancellationToken cancellationToken);
}

public class OrderService(IOrderRepository orders, IUnitOfWork unitOfWork) : IOrderService
{
    public async Task Test(CancellationToken cancellationToken)
    {
        int id = 2;

        var order = await orders.GetByIdAsync(id, cancellationToken); 
        if (order is null) 
            ArgumentNullException.ThrowIfNull($"Заказ с Id '{id}' не найден.");

        order?.NewOrder();
        order?.StartProcessing();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await ShipAsync(id, cancellationToken);
    }

    public async Task<OrderResponse?> GetOrderByIdAsync(int id, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(id, cancellationToken);
        return order is null ? null : MapToResponseDto(order);
    }

    public async Task<IEnumerable<OrderResponse>> GetAllOrdersAsync(
        GetOrdersQuery query, CancellationToken cancellationToken) =>
            (await orders.GetAllAsync(
                OrderStatus.FromValueOrNull(query.StatusId), cancellationToken))
            .Select(MapToResponseDto);

    private async Task<Email> EnsureEmailIsUniqueAsync(
        string emailValue,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var email = Email.Create(emailValue);

        if (await orders.EmailExistsAsync(email, excludeId, cancellationToken))
            throw new EmailAlreadyUsedException(email);

        return email;
    }
    public async Task<OrderResponse> CreateOrderAsync(CreateOrderDto dto, CancellationToken cancellationToken)
    {
        var customerName = new CustomerName(dto.CustomerName);
        var email = await EnsureEmailIsUniqueAsync(dto.Email, cancellationToken: cancellationToken);
        var price = new Money(dto.Price.Amount, dto.Price.Currency);

        var order = new Order(customerName, email, price);

        orders.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken); 

        return MapToResponseDto(order);
    }
    public async Task<OrderResponse?> UpdateOrderAsync(int id, UpdateOrderDto dto, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(id, cancellationToken);
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

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponseDto(order);
    }
    public async Task<bool> StartProcessingAsync(int id, CancellationToken cancellationToken) =>
         await ExecuteOrderActionAsync(id, order => order.StartProcessing(), cancellationToken);

    public async Task<bool> ShipAsync(int id, CancellationToken cancellationToken) =>
        await ExecuteOrderActionAsync(id, order => order.Ship(), cancellationToken);

    public async Task<bool> CancelAsync(int id, CancellationToken cancellationToken) =>
        await ExecuteOrderActionAsync(id, order => order.Cancel(), cancellationToken);

    public Task<bool> DeleteOrderAsync(int id, CancellationToken cancellationToken) =>
        ExecuteOrderActionAsync(id, order =>
        {
            order.Delete();
            orders.Remove(order);
        }, cancellationToken);

    private async Task<bool> ExecuteOrderActionAsync(
        int id,
        Action<Order> action,
        CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(id, cancellationToken);
        if (order is null) return false;

        action(order);

        await unitOfWork.SaveChangesAsync(cancellationToken);
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
