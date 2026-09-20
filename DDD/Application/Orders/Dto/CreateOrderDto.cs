using DDD.Domain.Enums;

namespace DDD.Application.Orders.Dto;

public class CreateOrderDto
{
    public string CustomerName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.New;
}
