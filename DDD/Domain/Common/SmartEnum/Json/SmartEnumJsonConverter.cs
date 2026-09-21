using System.Text.Json;
using System.Text.Json.Serialization;

namespace DDD.Domain.Common.SmartEnum.Json;

public class SmartEnumJsonConverter<T> : JsonConverter<T> where T : SmartEnum<T>, ISmartEnum<T>
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // СЦЕНАРИЙ 1: На вход пришло чистое число (например, 2)
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int intValue))
        {
            if (SmartEnum<T>.TryParse(intValue, out var result))
                return result;

            throw new JsonException(SmartEnum<T>.GetInvalidValueMessage(intValue));
        }

        // СЦЕНАРИЙ 2: На вход пришла строка (текст или число в кавычках)
        if (reader.TokenType == JsonTokenType.String)
        {
            string? rawValue = reader.GetString();

            if (string.IsNullOrWhiteSpace(rawValue))
                return null;

            // 2.1. Поиск по C# имени свойства (O(1)) -> например, "Processing"
            if (SmartEnum<T>.TryParse(rawValue, out var resultByName))
                return resultByName;

            // 2.2. Поиск по DisplayName (O(1)) -> например, "В обработке"
            if (SmartEnum<T>.TryParseByDisplay(rawValue, out var resultByDisplay))
                return resultByDisplay;

            // 2.3. Если строка — это число в кавычках (например, "2")
            if (int.TryParse(rawValue, out int parsedInt))
            {
                if (SmartEnum<T>.TryParse(parsedInt, out var resultByParsedInt))
                    return resultByParsedInt;

                throw new JsonException(SmartEnum<T>.GetInvalidValueMessage(parsedInt));
            }

            throw new JsonException(SmartEnum<T>.GetInvalidValueMessage(rawValue));
        }

        throw new JsonException($"Неподдерживаемый формат JSON-токена '{reader.TokenType}' для перечисления {typeToConvert.Name}.");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        // При сериализации API всегда отдает лаконичное числовое значение
        writer.WriteNumberValue(value.Value);
    }
}
