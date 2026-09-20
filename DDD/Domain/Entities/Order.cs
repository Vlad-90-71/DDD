using DDD.Domain.Enums;

namespace DDD.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    // Используем наш SmartEnum
    public OrderStatus Status { get; set; } = OrderStatus.New;
}