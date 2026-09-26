using System.Text.Json;
using DDD.Domain.Enums;
using DDD.Domain.Common.SmartEnum.Json;

namespace DDD.Tests.Domain;

public class SmartEnumJsonConverterFactoryTests
{
    [Fact]
    public void Factory_ShouldCreateConverterForSmartEnum()
    {
        var options = new JsonSerializerOptions();

        options.Converters.Add(new SmartEnumJsonConverterFactory());

        var json = JsonSerializer.Serialize(
            OrderStatus.Processing,
            options);

        Assert.Equal("2", json);
    }

    [Fact]
    public void Factory_ShouldDeserializeSmartEnum()
    {
        var options = new JsonSerializerOptions();

        options.Converters.Add(new SmartEnumJsonConverterFactory());

        var result = JsonSerializer.Deserialize<OrderStatus>(
            "3",
            options);

        Assert.Same(OrderStatus.Shipped, result);
    }

    [Fact]
    public void Factory_ShouldHandleNullableSmartEnum()
    {
        var options = new JsonSerializerOptions();

        options.Converters.Add(new SmartEnumJsonConverterFactory());

        var result = JsonSerializer.Deserialize<OrderStatus?>(
            "null",
            options);

        Assert.Null(result);
    }
}