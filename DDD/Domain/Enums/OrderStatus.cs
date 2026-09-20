using DDD.Domain.Common;

namespace DDD.Domain.Enums;

public class OrderStatus : SmartEnum<OrderStatus>
{
    public static readonly OrderStatus New = new(1, "Новый");
    public static readonly OrderStatus Processing = new(2, "В обработке");
    public static readonly OrderStatus Shipped = new(3, "Доставлен");
    public static readonly OrderStatus Canceled = new(4, "Отменен");

    private OrderStatus(int value, string displayName) : base(value, displayName) { }
}
