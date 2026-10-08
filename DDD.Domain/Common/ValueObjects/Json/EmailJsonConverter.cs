using System.Text.Json;
using System.Text.Json.Serialization;

namespace DDD.Domain.Common.ValueObjects.Json;

public sealed class EmailJsonConverter : JsonConverter<Email>
{
    public override Email Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Ожидался объект Email, получен {reader.TokenType}.");

        string? value = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Ожидалось имя свойства Email.");

            var propertyName = reader.GetString();

            if (!reader.Read())
                throw new JsonException("Неожиданный конец JSON при чтении Email.");

            if (string.Equals(propertyName, "value", StringComparison.OrdinalIgnoreCase))
                value = reader.GetString();
            else
                reader.Skip();
        }

        if (value is null)
            throw new JsonException("Email.Value отсутствует.");

        try
        {
            return Email.Create(value);
        }
        catch (ArgumentException ex)
        {
            throw new JsonException($"Некорректный Email: '{value}'.", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, Email value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("value", value.Value);
        writer.WriteEndObject();
    }
}