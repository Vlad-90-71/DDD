using Microsoft.EntityFrameworkCore;
using FluentValidation;
using DDD.Domain.Enums;
using DDD.Domain.Entities;
using DDD.Infrastructure;
using DDD.Application.Orders.Dto;

namespace DDD.Application.Orders;

public interface IOrderService
{
    Task<OrderResponseDto?> GetOrderByIdAsync(int id);
    Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync(GetOrdersQuery query); 
    Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto);
    Task<OrderResponseDto?> UpdateOrderAsync(int id, UpdateOrderDto dto);
    Task<bool> DeleteOrderAsync(int id);
}

public class OrderService(
    AppDbContext context,
    IValidator<GetOrdersQuery> queryValidator,
    IValidator<CreateOrderDto> createValidator,
    IValidator<UpdateOrderDto> updateValidator) : IOrderService 
{
    public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
    {
        var order = await context.Orders.FindAsync(id);
        if (order is null) return null;

        return MapToResponseDto(order);
    }

    // 1. ПОЛУЧЕНИЕ ВСЕХ ЗАКАЗОВ
    public async Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync(GetOrdersQuery query)
    {
        // 1. Принудительно валидируем параметры строки запроса
        var validationResult = await queryValidator.ValidateAsync(query);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        // 2. Начинаем строить запрос к базе данных
        var dbQuery = context.Orders.AsNoTracking();

        // 3. Если статус передан и он валиден, накладываем фильтр
        if (query.StatusId.HasValue)
        {
            var targetStatus = OrderStatus.FromValue(query.StatusId.Value);
            dbQuery = dbQuery.Where(o => o.Status == targetStatus);
        }

        var orders = await dbQuery.ToListAsync();

        return orders.Select(MapToResponseDto);
    }

    public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto)
    {
        var validationResult = await createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        var order = new Order(dto.CustomerName, dto.Status);

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        return MapToResponseDto(order);
    }

    // 2. ОБНОВЛЕНИЕ ЗАКАЗА (с мутацией доменного класса через методы)
    public async Task<OrderResponseDto?> UpdateOrderAsync(int id, UpdateOrderDto dto)
    {
        // Валидируем входящие данные
        var validationResult = await updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        // Ищем заказ в базе
        var order = await context.Orders.FindAsync(id);
        if (order is null) return null;

        order.UpdateCustomerName(dto.CustomerName);
        order.UpdateStatus(dto.Status);

        await context.SaveChangesAsync();

        return MapToResponseDto(order);
    }

    // 3. УДАЛЕНИЕ ЗАКАЗА
    public async Task<bool> DeleteOrderAsync(int id)
    {
        var order = await context.Orders.FindAsync(id);
        if (order is null) return false;

        context.Orders.Remove(order);
        await context.SaveChangesAsync();

        return true;
    }

    // Выносим маппинг в приватный хелпер для чистоты кода
    private static OrderResponseDto MapToResponseDto(Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            CustomerName = order.CustomerName,
            StatusId = order.Status.Value,
            StatusName = order.Status.ToString()
        };
    }
}
