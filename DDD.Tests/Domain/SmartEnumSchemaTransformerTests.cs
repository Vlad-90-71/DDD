using DDD.Domain.Entities.Order;
using DDD.OpenApi;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace DDD.Tests.Domain;

public sealed class SmartEnumSchemaTransformerTests
{
    private readonly SmartEnumSchemaTransformer _transformer = new();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
    }

    [Fact]
    public async Task OrderStatus_ShouldGenerateIntegerSchemaWithDescription()
    {
        // Arrange
        var schema = new OpenApiSchema();

        var options = CreateJsonOptions();

        var jsonTypeInfo =
            options.GetTypeInfo(typeof(OrderStatus));

        var context = new OpenApiSchemaTransformerContext
        {
            JsonTypeInfo = jsonTypeInfo,
            JsonPropertyInfo = null,
            DocumentName = "v1",
            ParameterDescription = null,
            ApplicationServices = new ServiceCollection()
                .BuildServiceProvider()
        };

        // Act
        await _transformer.TransformAsync(
            schema,
            context,
            CancellationToken.None);

        // Assert
        Assert.Equal("integer", schema.Type);
        Assert.Equal("int32", schema.Format);

        Assert.NotNull(schema.Description);
        Assert.Contains("1", schema.Description);
        Assert.Contains("2", schema.Description);
        Assert.Contains("3", schema.Description);
        Assert.Contains("4", schema.Description);

        // enum не должен содержать значения
        Assert.Empty(schema.Enum);

        Assert.False(schema.Nullable);
    }

    [Fact]
    public async Task NullableOrderStatus_ShouldGenerateIntegerSchemaWithDescriptionAndNullable()
    {
        // Arrange
        var schema = new OpenApiSchema();

        var options = CreateJsonOptions();

        var dtoTypeInfo =
            options.GetTypeInfo(typeof(TestDto));

        var propertyInfo =
            dtoTypeInfo.Properties
                .First(x => x.Name == nameof(TestDto.NullableStatus));

        var propertyTypeInfo =
            options.GetTypeInfo(propertyInfo.PropertyType);

        var context = new OpenApiSchemaTransformerContext
        {
            // ВАЖНО:
            // transformer анализирует context.JsonTypeInfo.Type
            // поэтому здесь должен быть JsonTypeInfo свойства,
            // а не JsonTypeInfo всего TestDto.
            JsonTypeInfo = propertyTypeInfo,
            JsonPropertyInfo = propertyInfo,
            DocumentName = "v1",
            ParameterDescription = null,
            ApplicationServices = new ServiceCollection()
                .BuildServiceProvider()
        };

        // Act
        await _transformer.TransformAsync(
            schema,
            context,
            CancellationToken.None);

        // Assert
        Assert.Equal("integer", schema.Type);
        Assert.Equal("int32", schema.Format);

        Assert.True(schema.Nullable);

        Assert.NotNull(schema.Description);
        Assert.Contains("1", schema.Description);
        Assert.Contains("2", schema.Description);
        Assert.Contains("3", schema.Description);
        Assert.Contains("4", schema.Description);

        // enum не должен содержать значения
        Assert.Empty(schema.Enum);
    }

    [Fact]
    public async Task String_ShouldNotBeChanged()
    {
        // Arrange
        var schema = new OpenApiSchema();

        var options = CreateJsonOptions();

        var jsonTypeInfo =
            options.GetTypeInfo(typeof(string));

        var context = new OpenApiSchemaTransformerContext
        {
            JsonTypeInfo = jsonTypeInfo,
            JsonPropertyInfo = null,
            DocumentName = "v1",
            ParameterDescription = null,
            ApplicationServices = new ServiceCollection()
                .BuildServiceProvider()
        };

        // Act
        await _transformer.TransformAsync(
            schema,
            context,
            CancellationToken.None);

        // Assert
        Assert.Null(schema.Type);
        Assert.Null(schema.Format);
        Assert.Null(schema.Description);

        // OpenApiSchema создаёт пустую коллекцию,
        // поэтому проверяем Empty, а не Null.
        Assert.Empty(schema.Enum);

        Assert.False(schema.Nullable);
    }

    private sealed class TestDto
    {
        public OrderStatus? NullableStatus { get; set; }
    }
}