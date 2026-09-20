using DDD.Domain.Enums;

namespace DDD.Application.Orders;

public class UpdateOrderDto
{
    public string CustomerName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.New;
}
