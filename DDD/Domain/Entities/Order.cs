using DDD.Domain.Enums;
using DDD.Domain.Common.Events;
using DDD.Domain.Common.ValueObjects;

namespace DDD.Domain.Entities;

public sealed record OrderProcessingStartedEvent(int OrderId) : IDomainEvent;
public sealed record OrderShippedEvent(int OrderId) : IDomainEvent;
public sealed record OrderCanceledEvent(int OrderId) : IDomainEvent;

public class Order
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public int Id { get; private set; }

    public CustomerName CustomerName { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public OrderStatus Status { get; private set; } = OrderStatus.New;

    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEvents.AsReadOnly();

    public Order(CustomerName customerName, Email email, Money price)
    {
        ArgumentNullException.ThrowIfNull(customerName);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(price);

        CustomerName = customerName;
        Email = email;
        Price = price;
    }

    private Order()
    {
    }
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

    public void NewOrder() => Status = OrderStatus.New;
    public void StartProcessing()
    {
        EnsureStatus(OrderStatus.New);

        Status = OrderStatus.Processing;

        _domainEvents.Add(new OrderProcessingStartedEvent(Id));
    }

    public void Ship()
    {
        EnsureStatus(OrderStatus.Processing);

        Status = OrderStatus.Shipped;

        _domainEvents.Add(new OrderShippedEvent(Id));
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

        _domainEvents.Add(new OrderCanceledEvent(Id));
    }
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
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