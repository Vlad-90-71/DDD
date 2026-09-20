using System.Text.Json;
using System.Text.Json.Serialization;
using DDD.Domain.Common;

namespace DDD.Presentation.Common.Json;

public class SmartEnumJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.BaseType is { IsGenericType: true } &&
               typeToConvert.BaseType.GetGenericTypeDefinition() == typeof(SmartEnum<>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(SmartEnumJsonConverter<>).MakeGenericType(typeToConvert);
        return Activator.CreateInstance(converterType) as JsonConverter;
    }
}
