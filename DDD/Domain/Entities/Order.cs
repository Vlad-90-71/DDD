using DDD.Domain.Common.ValueObjects;
using DDD.Domain.Enums;

namespace DDD.Domain.Entities;

public class Order
{
    public int Id { get; private set; }
    public CustomerName CustomerName { get; private set; } = null!;
    public OrderStatus Status { get; private set; } = OrderStatus.New;

    // Публичный конструктор для создания НОВЫХ заказов (Id не передается, он будет 0)
    public Order(CustomerName customerName, OrderStatus status)
    {
        CustomerName = customerName;
        Status = status;
    }

    // Скрытый конструктор специально для EF Core
    private Order() { }

    public void UpdateCustomerName(CustomerName customerName)
    {
        CustomerName = customerName;
    }
    public void UpdateStatus(OrderStatus newStatus)
    {
        Status = newStatus;
    }
}
