using DDD.Domain.Common.SmartEnum;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace DDD.Presentation.OpenApi;

public sealed class SmartEnumSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;

        if (!SmartEnum.IsSmartEnum(type))
            return Task.CompletedTask;

        schema.Type = "integer";
        schema.Format = "int32";
        
        var method = type.GetMethod("GetOpenApiDescription",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        
        schema.Description = (string) method!.Invoke(null, null)!;

        // OrderStatus? — nullable reference type.
        if (context.JsonPropertyInfo?.IsSetNullable == true)
            schema.Nullable = true;

        return Task.CompletedTask;
    }
}