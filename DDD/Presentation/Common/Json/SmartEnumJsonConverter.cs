using System.Text.Json;
using System.Text.Json.Serialization;
using DDD.Domain.Common;

namespace DDD.Presentation.Common.Json;

public class SmartEnumJsonConverter<T> : JsonConverter<T> where T : SmartEnum<T>, ISmartEnum<T>
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // СЦЕНАРИЙ 1: Если пришло число (например, 2)
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int intValue))
        {
            return T.FromValue(intValue);
        }

        // СЦЕНАРИЙ 2: Если пришла строка (например, "Processing" или "В обработке")
        if (reader.TokenType == JsonTokenType.String)
        {
            string? stringValue = reader.GetString();

            // 2.1 Пробуем распарсить по имени свойства C# (например, "Processing")
            if (T.TryParse(stringValue, out var resultByName))
            {
                return resultByName;
            }

            // 2.2 Если не вышло, пробуем распарсить по DisplayName (например, "В обработке")
            // Находим совпадение по переопределенному ToString()
            var resultByDisplay = T.GetAll()
                .FirstOrDefault(x => string.Equals(x.ToString(), stringValue, StringComparison.OrdinalIgnoreCase));

            if (resultByDisplay is not null)
            {
                return resultByDisplay;
            }

            // 2.3 Если клиент передал число, но обернул его в кавычки как строку: "2"
            if (int.TryParse(stringValue, out int parsedInt))
            {
                return T.FromValue(parsedInt);
            }
        }

        throw new JsonException($"Значение невалидно для перечисления {typeToConvert.Name}.");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);
        // При ответе API по-прежнему будет возвращать красивый лаконичный int
        writer.WriteNumberValue(value.Value);
    }
}
