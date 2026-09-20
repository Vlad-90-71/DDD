using System.Text.Json;
using System.Text.Json.Serialization;

namespace DDD.Domain.Common.SmartEnum.Json;

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
