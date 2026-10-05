using DDD.Domain.Common.Events;
using DDD.Domain.Common.ValueObjects;
using DDD.Domain.Enums;

namespace DDD.Domain.Entities.Order;

public class Order : EntityEvent
{
    public int Id { get; private set; }

    public string ProductName { get; private set; } = null!;

    public CustomerName CustomerName { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public OrderStatus Status { get; private set; } = OrderStatus.New;

    public Order(string productName, CustomerName customerName, Email email, Money price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        ArgumentNullException.ThrowIfNull(customerName);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(price);

        ProductName = productName;
        CustomerName = customerName;
        Email = email;
        Price = price;

        AddDomainEvent(new OrderCreatedEvent(ProductName, CustomerName, Email, Price));
    }

    private Order() { }

    public void RenameCustomer(string customerName)
    {
        EnsureOrderCanBeModified();

        CustomerName = new CustomerName(customerName);
    }

    public void ChangeEmail(Email email)
    {
        ArgumentNullException.ThrowIfNull(email);
        EnsureOrderCanBeModified();

        Email = email;
    }

    public void ChangePrice(Money price)
    {
        ArgumentNullException.ThrowIfNull(price);
        EnsureOrderCanBeModified();

        Price = price;
    }

    public void ChangePriceAmount(decimal amount)
    {
        EnsureOrderCanBeModified();

        Price = new Money(amount, Price.Currency);
    }

    public void SetOrderStatusNew()
    {
        Status = OrderStatus.New;
    }

    public void StartProcessing(string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        EnsureStatus(OrderStatus.New);

        AddDomainEvent(new OrderProcessingRequestedEvent(Id, ProductName, address));
    }

    public void Process()
    {
        EnsureStatus(OrderStatus.New);

        Status = OrderStatus.Processing;

        AddDomainEvent(new OrderProcessingStartedEvent(Id, ProductName));
    }

    public void Ship()
    {
        EnsureStatus(OrderStatus.Processing);

        Status = OrderStatus.Shipped;

        AddDomainEvent(new OrderShippedEvent(Id));
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Shipped)
            throw new InvalidOperationException(
                "Нельзя отменить уже доставленный заказ.");

        if (Status == OrderStatus.Canceled)
            throw new InvalidOperationException(
                "Заказ уже отменен.");

        Status = OrderStatus.Canceled;

        AddDomainEvent(new OrderCanceledEvent(Id));
    }

    public void Delete()
    {
        if (Status == OrderStatus.Processing)
            throw new InvalidOperationException(
                "Нельзя удалить заказ, который уже находится в обработке.");

        if (Status == OrderStatus.Shipped)
            throw new InvalidOperationException(
                "Нельзя удалить доставленный заказ.");
    }

    private void EnsureOrderCanBeModified()
    {
        if (Status == OrderStatus.Shipped)
            throw new InvalidOperationException(
                "Нельзя изменить доставленный заказ.");

        if (Status == OrderStatus.Canceled)
            throw new InvalidOperationException(
                "Нельзя изменить отмененный заказ.");
    }

    private void EnsureStatus(OrderStatus expectedStatus)
    {
        if (Status != expectedStatus)
            throw new InvalidOperationException(
                $"Операция недоступна для заказа со статусом '{Status}'. " +
                $"Ожидаемый статус: '{expectedStatus}'.");
    }
}