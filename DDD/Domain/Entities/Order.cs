using DDD.Domain.Enums;

namespace DDD.Domain.Entities;

public class Order
{
    public int Id { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; } = OrderStatus.New;

    // Публичный конструктор для создания НОВЫХ заказов (Id не передается, он будет 0)
    public Order(string customerName, OrderStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);
        CustomerName = customerName;
        Status = status;
    }

    // Скрытый конструктор специально для EF Core
    private Order() { }

    // Контролируемое изменение состояния через метод (бизнес-логика)
    public void UpdateCustomerName(string customerName)
    {
        // Здесь можно добавить проверку правил: например, нельзя отменить доставленный заказ
        if (!string.IsNullOrWhiteSpace(customerName))
            CustomerName = customerName;
    }
    public void UpdateStatus(OrderStatus newStatus)
    {
        Status = newStatus;
    }
}
