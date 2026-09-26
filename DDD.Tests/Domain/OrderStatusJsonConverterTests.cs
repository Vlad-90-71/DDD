using System.Text.Json;
using DDD.Domain.Common.SmartEnum.Json;
using DDD.Domain.Enums;

namespace DDD.Tests.Domain;

public class OrderStatusJsonConverterTests
{
    private readonly JsonSerializerOptions _options = new();

    public OrderStatusJsonConverterTests()
    {
        _options.Converters.Add(new SmartEnumJsonConverterFactory());
    }

    [Fact]
    public void Serialize_ShouldWriteNumericValue()
    {
        var status = OrderStatus.New;

        var json = JsonSerializer.Serialize(status, _options);

        Assert.Equal("1", json);
    }

    [Fact]
    public void Deserialize_FromNumber_ShouldReturnCorrectStatus()
    {
        var status = JsonSerializer.Deserialize<OrderStatus>(
            "1",
            _options);

        Assert.Same(OrderStatus.New, status);
    }

    [Fact]
    public void Deserialize_FromStringName_ShouldReturnCorrectStatus()
    {
        var status = JsonSerializer.Deserialize<OrderStatus>(
            "\"Processing\"",
            _options);

        Assert.Same(OrderStatus.Processing, status);
    }

    [Fact]
    public void Deserialize_FromDisplayName_ShouldReturnCorrectStatus()
    {
        var status = JsonSerializer.Deserialize<OrderStatus>(
            "\"В обработке\"",
            _options);

        Assert.Same(OrderStatus.Processing, status);
    }

    [Fact]
    public void Deserialize_FromNumericString_ShouldReturnCorrectStatus()
    {
        var status = JsonSerializer.Deserialize<OrderStatus>(
            "\"3\"",
            _options);

        Assert.Same(OrderStatus.Shipped, status);
    }

    [Fact]
    public void Deserialize_UnknownNumber_ShouldThrowJsonException()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<OrderStatus>(
                "999",
                _options));
    }

    [Fact]
    public void Deserialize_UnknownString_ShouldThrowJsonException()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<OrderStatus>(
                "\"Unknown\"",
                _options));
    }

    [Fact]
    public void Deserialize_WhitespaceString_ShouldReturnNull_ForNullableStatus()
    {
        var status = JsonSerializer.Deserialize<OrderStatus?>(
            "\"   \"",
            _options);

        Assert.Null(status);
    }

    [Fact]
    public void Serialize_NullableStatusNull_ShouldWriteNull()
    {
        OrderStatus? status = null;

        var json = JsonSerializer.Serialize(status, _options);

        Assert.Equal("null", json);
    }

    [Fact]
    public void Deserialize_Null_ShouldReturnNull_ForNullableStatus()
    {
        var status = JsonSerializer.Deserialize<OrderStatus?>(
            "null",
            _options);

        Assert.Null(status);
    }

    [Fact]
    public void Serialize_ShouldWorkForAllStatuses()
    {
        var statuses = new[]
        {
            OrderStatus.New,
            OrderStatus.Processing,
            OrderStatus.Shipped,
            OrderStatus.Canceled
        };

        var expected = new[]
        {
            "1",
            "2",
            "3",
            "4"
        };

        for (var i = 0; i < statuses.Length; i++)
        {
            var json = JsonSerializer.Serialize(
                statuses[i],
                _options);

            Assert.Equal(expected[i], json);
        }
    }
}