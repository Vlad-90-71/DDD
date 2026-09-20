namespace DDD.Application.Orders;

public class CreateOrderDto
{
    public string CustomerName { get; set; } = string.Empty;
    public int StatusId { get; set; } // Получаем статус в виде обычного int
}
