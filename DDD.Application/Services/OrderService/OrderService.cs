using DDD.Application.Common;
using DDD.Domain.Common.ValueObjects;
using DDD.Domain.Entities.Order;
using DDD.Eventing.Contracts;
using System.Data;

namespace DDD.Application.Services.OrderService;

public sealed class EmailAlreadyUsedException(Email email) :
    Exception($"Email '{email.Value}' уже используется.") {}

public class OrderService(IOrderRepository orders, IUnitOfWork unitOfWork, IFailureSimulator failureSimulator) : IOrderService
{
    public async Task Test(CancellationToken cancellationToken)
    {
        var dto = new CreateOrderDto(
            productName: "Test Product",
            customerName: "Test Customer",
            email: $"test-{Guid.NewGuid():N}@example.com",
            price: new MoneyDto(100m, "EUR"));

        var order = await CreateOrderAsync(
            dto,
            cancellationToken);

        Console.WriteLine(
            $"Order создан: Id={order.Id}, " +
            $"Status='{order.Status}'");

        await StartProcessingAsync(
            order.Id,
            "Test Address",
            cancellationToken);

        Console.WriteLine(
            $"Запрос на обработку заказа {order.Id} отправлен.");

        await WaitForOrderStatusAsync(
            order.Id,
            OrderStatus.Processing,
            cancellationToken);

        await orders.ReloadAsync(
            order.Id,
            cancellationToken);

        Console.WriteLine(
            $"Заказ {order.Id} находится в обработке.");

        await ShipAsync(
            order.Id,
            cancellationToken);

        await WaitForOrderStatusAsync(
            order.Id,
            OrderStatus.Shipped,
            cancellationToken);

        await orders.ReloadAsync(
            order.Id,
            cancellationToken);

        Console.WriteLine(
            $"Заказ {order.Id} доставлен.");

        await Task.Delay(
            TimeSpan.FromSeconds(2),
            cancellationToken);
    }

    private async Task WaitForOrderStatusAsync(
        int id,
        OrderStatus expectedStatus,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 50;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var order = await orders.GetByIdReadOnlyAsync(
                id,
                cancellationToken);

            if (order?.Status == expectedStatus)
                return;

            await Task.Delay(
                TimeSpan.FromMilliseconds(100),
                cancellationToken);
        }

        throw new TimeoutException(
            $"Заказ {id} не перешел в статус '{expectedStatus}'.");
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

        var order = new Order(dto.ProductName, customerName, email, price);

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
    public async Task<bool> StartProcessingAsync(int id, string address, CancellationToken cancellationToken) =>
         await ExecuteOrderActionAsync(id, order => order.StartProcessing(address), cancellationToken);

    public Task HandleOrderShippedAsync(OrderShippedEvent domainEvent, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Обработка события доставки заказа {domainEvent.OrderId}");

        return Task.CompletedTask;
    }

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

    private async Task<bool> ExecuteOrderActionAsync(int id, Action<Order> action, CancellationToken cancellationToken)
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
            order.ProductName,
            order.CustomerName.Value, 
            order.Email.Value, 
            new MoneyDto(order.Price.Amount, order.Price.Currency), 
            order.Status.ToString());
}
