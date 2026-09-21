using System.Text.Json;
using System.Text.Json.Serialization;

namespace DDD.Domain.Common.SmartEnum.Json;
/*
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
*/

public sealed class SmartEnumJsonConverterFactory : JsonConverterFactory
{
    private static readonly Type ConverterType =
        typeof(SmartEnumJsonConverter<>);

    public override bool CanConvert(Type typeToConvert)
    {
        var type = Nullable.GetUnderlyingType(typeToConvert)
                   ?? typeToConvert;

        return IsSmartEnum(type);
    }

    public override JsonConverter CreateConverter(
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var type = Nullable.GetUnderlyingType(typeToConvert)
                   ?? typeToConvert;

        if (!IsSmartEnum(type))
        {
            throw new InvalidOperationException(
                $"Тип '{typeToConvert}' не является SmartEnum<T>.");
        }

        var converterType = ConverterType.MakeGenericType(type);

        return (JsonConverter)Activator.CreateInstance(
            converterType)!;
    }

    private static bool IsSmartEnum(Type type)
    {
        return FindSmartEnumBase(type) is not null;
    }

    private static Type? FindSmartEnumBase(Type type)
    {
        for (var current = type;
             current is not null && current != typeof(object);
             current = current.BaseType)
        {
            if (current.IsGenericType &&
                current.GetGenericTypeDefinition() == typeof(SmartEnum<>))
            {
                return current;
            }
        }

        return null;
    }
}