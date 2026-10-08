using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;

namespace DDD.Domain.Common.SmartEnum.Json;

public sealed class SmartEnumJsonConverterFactory : JsonConverterFactory
{
    // Кэш для созданных конвертеров, чтобы избежать постоянного MakeGenericType и Activator
    private static readonly ConcurrentDictionary<Type, JsonConverter> ConverterCache = new();

    public override bool CanConvert(Type typeToConvert)
    {
        var type = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
        return IsSmartEnum(type);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var type = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;

        // Используем фабрику внутри GetOrAdd для потокобезопасного кэширования
        return ConverterCache.GetOrAdd(type, t =>
        {
            if (!IsSmartEnum(type))
                throw new InvalidOperationException($"Тип '{t}' не является SmartEnum<T>.");

            var converterType = typeof(SmartEnumJsonConverter<>).MakeGenericType(type);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        });
    }

    private static bool IsSmartEnum(Type type)
    {
        // Проверяем иерархию типов более современным способом
        for (var current = type; 
            current is not null && current != typeof(object);
            current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(SmartEnum<>))
                return true;
        }

        return false;
    }
}
