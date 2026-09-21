using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DDD.Domain.Common.SmartEnum.Json;

public sealed class SmartEnumJsonConverter<T> : JsonConverter<T>
    where T : SmartEnum<T>, ISmartEnum<T>
{
    public override T? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                {
                    if (!reader.TryGetInt32(out var value))
                    {
                        throw new JsonException(
                            $"Значение для {typeof(T).Name} должно быть целым числом.");
                    }

                    return ParseValue(value);
                }

            case JsonTokenType.String:
                {
                    var rawValue = reader.GetString();

                    if (string.IsNullOrWhiteSpace(rawValue))
                        return null;

                    // C# field name:
                    // "New", "Processing", "Shipped", "Canceled"
                    if (SmartEnum<T>.TryParse(rawValue, out var resultByName))
                        return resultByName;

                    // DisplayName:
                    // "Новый", "В обработке", "Доставлен", "Отменен"
                    if (SmartEnum<T>.TryParseByDisplay(rawValue, out var resultByDisplay))
                        return resultByDisplay;

                    // Numeric string:
                    // "1", "2", "3", "4"
                    if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
                        return ParseValue(parsedValue);

                    throw new JsonException(
                        SmartEnum<T>.GetInvalidValueMessage(rawValue));
                }

            case JsonTokenType.Null:
                return null;

            default:
                throw new JsonException(
                    $"Неподдерживаемый JSON-токен '{reader.TokenType}' " +
                    $"для {typeof(T).Name}.");
        }
    }

    public override void Write(
        Utf8JsonWriter writer,
        T value,
        JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }

    private static T ParseValue(int value)
    {
        if (SmartEnum<T>.TryParse(value, out var result))
            return result;

        throw new JsonException(
            SmartEnum<T>.GetInvalidValueMessage(value));
    }
}