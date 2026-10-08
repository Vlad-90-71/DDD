using DDD.Domain.Common.ValueObjects;
using DDD.Domain.Entities.Order;

namespace DDD.Tests.Domain;

public class OrderTests
{
    private static Order CreateOrder(
        decimal amount = 100m,
        string currency = "EUR")
    {
        return new Order(
            "Адрес ул 55",
            new CustomerName("Иван Иванов"),
            Email.Create("ivan@example.com"),
            new Money(amount, currency));
    }

    [Fact]
    public void Constructor_ShouldCreateOrderWithNewStatus()
    {
        // Arrange
        var customerName = new CustomerName("Иван Иванов");
        var email = Email.Create("ivan@example.com");
        var price = new Money(100m, "EUR");

        // Act
        var order = new Order(
            "Адрес ул 55",
            customerName,
            email,
            price);

        // Assert
        Assert.Equal(customerName, order.CustomerName);
        Assert.Equal(email, order.Email);
        Assert.Equal(price, order.Price);
        Assert.Equal(OrderStatus.New, order.Status);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCustomerNameIsNull()
    {
        // Arrange
        var email = Email.Create("ivan@example.com");
        var price = new Money(100m, "EUR");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new Order(
                "",
                null!,
                email,
                price));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenEmailIsNull()
    {
        // Arrange
        var customerName = new CustomerName("Иван Иванов");
        var price = new Money(100m, "EUR");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new Order(
                "Адрес ул 55",
                customerName,
                null!,
                price));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenPriceIsNull()
    {
        // Arrange
        var customerName = new CustomerName("Иван Иванов");
        var email = Email.Create("ivan@example.com");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new Order(
                "Адрес ул 55",
                customerName,
                email,
                null!));
    }

    // -------------------------------------------------------
    // RenameCustomer
    // -------------------------------------------------------

    [Fact]
    public void RenameCustomer_ShouldChangeCustomerName()
    {
        // Arrange
        var order = CreateOrder();

        // Act
        order.RenameCustomer("Петр Петров");

        // Assert
        Assert.Equal(
            new CustomerName("Петр Петров"),
            order.CustomerName);
    }

    [Fact]
    public void RenameCustomer_ShouldThrow_WhenOrderIsShipped()
    {
        // Arrange
        var order = CreateOrder();

        order.StartProcessing("Адрес ул 55");
        order.Ship();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            order.RenameCustomer("Петр Петров"));

        Assert.Equal(
            "Нельзя изменить доставленный заказ.",
            exception.Message);
    }

    [Fact]
    public void RenameCustomer_ShouldThrow_WhenOrderIsCanceled()
    {
        // Arrange
        var order = CreateOrder();

        order.Cancel();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            order.RenameCustomer("Петр Петров"));

        Assert.Equal(
            "Нельзя изменить отмененный заказ.",
            exception.Message);
    }

    // -------------------------------------------------------
    // ChangeEmail
    // -------------------------------------------------------

    [Fact]
    public void ChangeEmail_ShouldChangeEmail()
    {
        // Arrange
        var order = CreateOrder();
        var newEmail = Email.Create("petr@example.com");

        // Act
        order.ChangeEmail(newEmail);

        // Assert
        Assert.Equal(newEmail, order.Email);
    }

    [Fact]
    public void ChangeEmail_ShouldThrow_WhenEmailIsNull()
    {
        // Arrange
        var order = CreateOrder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            order.ChangeEmail(null!));
    }

    [Fact]
    public void ChangeEmail_ShouldThrow_WhenOrderIsShipped()
    {
        // Arrange
        var order = CreateOrder();

        order.StartProcessing("Адрес ул 55");
        order.Ship();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            order.ChangeEmail(Email.Create("new@example.com")));

        Assert.Equal(
            "Нельзя изменить доставленный заказ.",
            exception.Message);
    }

    [Fact]
    public void ChangeEmail_ShouldThrow_WhenOrderIsCanceled()
    {
        // Arrange
        var order = CreateOrder();

        order.Cancel();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            order.ChangeEmail(Email.Create("new@example.com")));

        Assert.Equal(
            "Нельзя изменить отмененный заказ.",
            exception.Message);
    }

    // -------------------------------------------------------
    // ChangePrice
    // -------------------------------------------------------

    [Fact]
    public void ChangePrice_ShouldChangePrice()
    {
        // Arrange
        var order = CreateOrder();
        var newPrice = new Money(250m, "EUR");

        // Act
        order.ChangePrice(newPrice);

        // Assert
        Assert.Equal(newPrice, order.Price);
    }

    [Fact]
    public void ChangePrice_ShouldThrow_WhenPriceIsNull()
    {
        // Arrange
        var order = CreateOrder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            order.ChangePrice(null!));
    }

    [Fact]
    public void ChangePrice_ShouldThrow_WhenOrderIsShipped()
    {
        // Arrange
        var order = CreateOrder();

        order.StartProcessing("Адрес ул 55");
        order.Ship();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            order.ChangePrice(new Money(250m, "EUR")));
    }

    [Fact]
    public void ChangePrice_ShouldThrow_WhenOrderIsCanceled()
    {
        // Arrange
        var order = CreateOrder();

        order.Cancel();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            order.ChangePrice(new Money(250m, "EUR")));
    }

    // -------------------------------------------------------
    // ChangePriceAmount
    // -------------------------------------------------------

    [Fact]
    public void ChangePriceAmount_ShouldChangeAmountAndKeepCurrency()
    {
        // Arrange
        var order = CreateOrder(100m, "EUR");

        // Act
        order.ChangePriceAmount(250m);

        // Assert
        Assert.Equal(
            new Money(250m, "EUR"),
            order.Price);
    }

    [Fact]
    public void ChangePriceAmount_ShouldThrow_WhenOrderIsShipped()
    {
        // Arrange
        var order = CreateOrder();

        order.StartProcessing("Адрес ул 55");
        order.Ship();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            order.ChangePriceAmount(250m));
    }

    [Fact]
    public void ChangePriceAmount_ShouldThrow_WhenOrderIsCanceled()
    {
        // Arrange
        var order = CreateOrder();

        order.Cancel();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            order.ChangePriceAmount(250m));
    }

    // -------------------------------------------------------
    // StartProcessing
    // -------------------------------------------------------

    [Fact]
    public void StartProcessing_ShouldChangeStatusToProcessing()
    {
        // Arrange
        var order = CreateOrder();

        // Act
        order.StartProcessing("Адрес ул 55");

        // Assert
        Assert.Equal(
            OrderStatus.Processing,
            order.Status);
    }

    [Fact]
    public void StartProcessing_ShouldThrow_WhenOrderIsProcessing()
    {
        // Arrange
        var order = CreateOrder();

        order.StartProcessing("Адрес ул 55");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            order.StartProcessing("Адрес ул 55"));
    }

    [Fact]
    public void StartProcessing_ShouldThrow_WhenOrderIsShipped()
    {
        // Arrange
        var order = CreateOrder();

        order.StartProcessing("Адрес ул 55");
        order.Ship();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            order.StartProcessing("Адрес ул 55"));
    }

    [Fact]
    public void StartProcessing_ShouldThrow_WhenOrderIsCanceled()
    {
        // Arrange
        var order = CreateOrder();

        order.Cancel();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            order.StartProcessing("Адрес ул 55"));
    }

    // -------------------------------------------------------
    // Ship
    // -------------------------------------------------------

    [Fact]
    public void Ship_ShouldChangeStatusToShipped()
    {
        // Arrange
        var order = CreateOrder();

        order.StartProcessing("Адрес ул 55");

        // Act
        order.Ship();

        // Assert
        Assert.Equal(
            OrderStatus.Shipped,
            order.Status);
    }

    [Fact]
    public void Ship_ShouldThrow_WhenOrderIsNew()
    {
        // Arrange
        var order = CreateOrder();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            order.Ship());
    }

    [Fact]
    public void Ship_ShouldThrow_WhenOrderIsShipped()
    {
        // Arrange
        var order = CreateOrder();

        order.StartProcessing("Адрес ул 55");
        order.Ship();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            order.Ship());
    }

    [Fact]
    public void Ship_ShouldThrow_WhenOrderIsCanceled()
    {
        // Arrange
        var order = CreateOrder();

        order.Cancel();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            order.Ship());
    }

    // -------------------------------------------------------
    // Cancel
    // -------------------------------------------------------

    [Fact]
    public void Cancel_ShouldChangeStatusToCanceled_FromNew()
    {
        // Arrange
        var order = CreateOrder();

        // Act
        order.Cancel();

        // Assert
        Assert.Equal(
            OrderStatus.Canceled,
            order.Status);
    }

    [Fact]
    public void Cancel_ShouldChangeStatusToCanceled_FromProcessing()
    {
        // Arrange
        var order = CreateOrder();

        order.StartProcessing("Адрес ул 55");

        // Act
        order.Cancel();

        // Assert
        Assert.Equal(
            OrderStatus.Canceled,
            order.Status);
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenOrderIsShipped()
    {
        // Arrange
        var order = CreateOrder();

        order.StartProcessing("Адрес ул 55");
        order.Ship();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            order.Cancel());

        Assert.Equal(
            "Нельзя отменить уже доставленный заказ.",
            exception.Message);
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenOrderIsAlreadyCanceled()
    {
        // Arrange
        var order = CreateOrder();

        order.Cancel();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            order.Cancel());

        Assert.Equal(
            "Заказ уже отменен.",
            exception.Message);
    }

    // -------------------------------------------------------
    // Delete
    // -------------------------------------------------------

    [Fact]
    public void Delete_ShouldNotThrow_WhenOrderIsNew()
    {
        // Arrange
        var order = CreateOrder();

        // Act & Assert
        order.Delete();
    }

    [Fact]
    public void Delete_ShouldNotThrow_WhenOrderIsCanceled()
    {
        // Arrange
        var order = CreateOrder();

        order.Cancel();

        // Act & Assert
        order.Delete();
    }

    [Fact]
    public void Delete_ShouldThrow_WhenOrderIsProcessing()
    {
        // Arrange
        var order = CreateOrder();

        order.StartProcessing("Адрес ул 55");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            order.Delete());

        Assert.Equal(
            "Нельзя удалить заказ, который уже находится в обработке.",
            exception.Message);
    }

    [Fact]
    public void Delete_ShouldThrow_WhenOrderIsShipped()
    {
        // Arrange
        var order = CreateOrder();

        order.StartProcessing("Адрес ул 55");
        order.Ship();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            order.Delete());

        Assert.Equal(
            "Нельзя удалить доставленный заказ.",
            exception.Message);
    }
}