using DDD.Domain.Common.ValueObjects;
using DDD.Domain.Entities;
using DDD.Domain.Enums;

namespace DDD.Tests.Domain;

public class OrderTests
{
    [Fact]
    public void Constructor_ShouldCreateOrderWithNewStatus()
    {
        // Arrange
        var customerName = new CustomerName("Иван Иванов");
        var email = Email.Create("ivan@example.com");
        var price = new Money(100m, "EUR");

        // Act
        var order = new Order(
            customerName,
            email,
            price);

        // Assert
        Assert.Equal(customerName, order.CustomerName);
        Assert.Equal(email, order.Email);
        Assert.Equal(price, order.Price);
        Assert.Equal(OrderStatus.New, order.Status);
    }
}