namespace DDD.Application.Services.OrderService;

public record GetOrdersQuery(int? StatusId);
public record OrderResponse(int Id, string ProductName, string CustomerName, string Email, MoneyDto Price, string Status);

public record MoneyDto(decimal Amount, string Currency);
public record UpdateMoneyDto(decimal? Amount = null, string? Currency = null);

public class CreateOrderDto(string productName, string customerName, string email, MoneyDto price)
{
    public string ProductName { get; set; } = productName;
    public string CustomerName { get; set; } = customerName;
    public string Email { get; set; } = email;
    public MoneyDto Price { get; set; } = price;
}

public class UpdateOrderDto(string? customerName = null, string? email = null, UpdateMoneyDto? price = null)
{
    public string? CustomerName { get; set; } = customerName;
    public string? Email { get; set; } = email;
    public UpdateMoneyDto? Price { get; set; } = price;
}
