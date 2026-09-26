using System.Reflection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using DDD.Domain.Common.SmartEnum;

namespace DDD.OpenApi;

public sealed class SmartEnumSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var originalType = context.JsonTypeInfo.Type;

        var underlyingType = Nullable.GetUnderlyingType(originalType);
        var type = underlyingType ?? originalType;

        if (!SmartEnum.IsSmartEnum(type))
        {
            return Task.CompletedTask;
        }

        schema.Type = "integer";
        schema.Format = "int32";

        // Для value type Nullable<T>
        if (underlyingType is not null)
        {
            schema.Nullable = true;
        }

        // Для nullable reference type: OrderStatus?
        if (context.JsonPropertyInfo?.IsSetNullable == true)
        {
            schema.Nullable = true;
        }

        var method = type.GetMethod(
            "GetOpenApiDescription",
            BindingFlags.Public |
            BindingFlags.Static |
            BindingFlags.FlattenHierarchy);

        if (method is not null)
        {
            schema.Description = method.Invoke(null, null) as string;
        }

        return Task.CompletedTask;
    }
}