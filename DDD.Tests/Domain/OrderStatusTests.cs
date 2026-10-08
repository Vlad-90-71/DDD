using DDD.Domain.Entities.Order;

namespace DDD.Tests.Domain;

public class OrderStatusTests
{
    [Fact]
    public void Values_ShouldHaveExpectedValues()
    {
        Assert.Equal(1, OrderStatus.New.Value);
        Assert.Equal(2, OrderStatus.Processing.Value);
        Assert.Equal(3, OrderStatus.Shipped.Value);
        Assert.Equal(4, OrderStatus.Canceled.Value);
    }

    [Fact]
    public void ToString_ShouldReturnDisplayName()
    {
        Assert.Equal("Новый", OrderStatus.New.ToString());
        Assert.Equal("В обработке", OrderStatus.Processing.ToString());
        Assert.Equal("Доставлен", OrderStatus.Shipped.ToString());
        Assert.Equal("Отменен", OrderStatus.Canceled.ToString());
    }

    [Fact]
    public void FromValue_ShouldReturnCorrectStatus()
    {
        Assert.Same(OrderStatus.New, OrderStatus.FromValue(1));
        Assert.Same(OrderStatus.Processing, OrderStatus.FromValue(2));
        Assert.Same(OrderStatus.Shipped, OrderStatus.FromValue(3));
        Assert.Same(OrderStatus.Canceled, OrderStatus.FromValue(4));
    }

    [Fact]
    public void FromValue_ShouldThrowForUnknownValue()
    {
        var exception = Assert.Throws<InvalidCastException>(
            () => OrderStatus.FromValue(999));

        Assert.Contains("невалидно", exception.Message);
        Assert.Contains("OrderStatus", exception.Message);
    }

    [Fact]
    public void TryParse_ByName_ShouldReturnCorrectStatus()
    {
        Assert.True(OrderStatus.TryParse("New", out var newStatus));
        Assert.Same(OrderStatus.New, newStatus);

        Assert.True(OrderStatus.TryParse("Processing", out var processingStatus));
        Assert.Same(OrderStatus.Processing, processingStatus);

        Assert.True(OrderStatus.TryParse("Shipped", out var shippedStatus));
        Assert.Same(OrderStatus.Shipped, shippedStatus);

        Assert.True(OrderStatus.TryParse("Canceled", out var canceledStatus));
        Assert.Same(OrderStatus.Canceled, canceledStatus);
    }

    [Fact]
    public void TryParse_ByName_ShouldBeCaseInsensitive()
    {
        Assert.True(OrderStatus.TryParse("new", out var status));

        Assert.Same(OrderStatus.New, status);
    }

    [Fact]
    public void TryParse_ByName_ShouldReturnFalseForUnknownName()
    {
        var result = OrderStatus.TryParse("Unknown", out var status);

        Assert.False(result);
        Assert.Null(status);
    }

    [Fact]
    public void TryParse_ByName_ShouldReturnFalseForNullOrWhitespace()
    {
        Assert.False(OrderStatus.TryParse(null, out var nullStatus));
        Assert.Null(nullStatus);

        Assert.False(OrderStatus.TryParse("", out var emptyStatus));
        Assert.Null(emptyStatus);

        Assert.False(OrderStatus.TryParse("   ", out var whitespaceStatus));
        Assert.Null(whitespaceStatus);
    }

    [Fact]
    public void TryParseByDisplay_ShouldReturnCorrectStatus()
    {
        Assert.True(OrderStatus.TryParseByDisplay("Новый", out var newStatus));
        Assert.Same(OrderStatus.New, newStatus);

        Assert.True(OrderStatus.TryParseByDisplay("В обработке", out var processingStatus));
        Assert.Same(OrderStatus.Processing, processingStatus);

        Assert.True(OrderStatus.TryParseByDisplay("Доставлен", out var shippedStatus));
        Assert.Same(OrderStatus.Shipped, shippedStatus);

        Assert.True(OrderStatus.TryParseByDisplay("Отменен", out var canceledStatus));
        Assert.Same(OrderStatus.Canceled, canceledStatus);
    }

    [Fact]
    public void TryParseByDisplay_ShouldBeCaseInsensitive()
    {
        Assert.True(OrderStatus.TryParseByDisplay("новый", out var status));

        Assert.Same(OrderStatus.New, status);
    }

    [Fact]
    public void TryParseByDisplay_ShouldReturnFalseForUnknownDisplay()
    {
        var result = OrderStatus.TryParseByDisplay("Неизвестный статус", out var status);

        Assert.False(result);
        Assert.Null(status);
    }

    [Fact]
    public void TryParse_ByValue_ShouldReturnCorrectStatus()
    {
        Assert.True(OrderStatus.TryParse(1, out var newStatus));
        Assert.Same(OrderStatus.New, newStatus);

        Assert.True(OrderStatus.TryParse(2, out var processingStatus));
        Assert.Same(OrderStatus.Processing, processingStatus);

        Assert.True(OrderStatus.TryParse(3, out var shippedStatus));
        Assert.Same(OrderStatus.Shipped, shippedStatus);

        Assert.True(OrderStatus.TryParse(4, out var canceledStatus));
        Assert.Same(OrderStatus.Canceled, canceledStatus);
    }

    [Fact]
    public void TryParse_ByValue_ShouldReturnFalseForUnknownValue()
    {
        var result = OrderStatus.TryParse(999, out var status);

        Assert.False(result);
        Assert.Null(status);
    }

    [Fact]
    public void GetAll_ShouldReturnAllStatuses()
    {
        var statuses = OrderStatus.GetAll().ToArray();

        Assert.Equal(4, statuses.Length);

        Assert.Contains(OrderStatus.New, statuses);
        Assert.Contains(OrderStatus.Processing, statuses);
        Assert.Contains(OrderStatus.Shipped, statuses);
        Assert.Contains(OrderStatus.Canceled, statuses);
    }

    [Fact]
    public void Equality_ShouldCompareByValue()
    {
        Assert.Equal(OrderStatus.New, OrderStatus.New);
        Assert.NotEqual(OrderStatus.New, OrderStatus.Processing);

        var status = OrderStatus.New;
        Assert.True(OrderStatus.New == status);
        Assert.True(OrderStatus.New != OrderStatus.Canceled);
    }

    [Fact]
    public void GetOpenApiDescription_ShouldContainAllStatuses()
    {
        var description = OrderStatus.GetOpenApiDescription();

        Assert.Contains("Доступные значения:", description);

        Assert.Contains("**1**", description);
        Assert.Contains("New", description);
        Assert.Contains("Новый", description);

        Assert.Contains("**2**", description);
        Assert.Contains("Processing", description);
        Assert.Contains("В обработке", description);

        Assert.Contains("**3**", description);
        Assert.Contains("Shipped", description);
        Assert.Contains("Доставлен", description);

        Assert.Contains("**4**", description);
        Assert.Contains("Canceled", description);
        Assert.Contains("Отменен", description);
    }
}