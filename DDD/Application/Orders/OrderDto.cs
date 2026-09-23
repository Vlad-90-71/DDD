namespace DDD.Application.Orders;

public record MoneyDto(decimal Amount, string Currency);
public record UpdateMoneyDto(decimal? Amount = null, string? Currency = null);

public class CreateOrderDto(string customerName, string email, MoneyDto price)
{
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