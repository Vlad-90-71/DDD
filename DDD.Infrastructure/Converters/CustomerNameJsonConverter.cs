using System.Text.Json;
using System.Text.Json.Serialization;
using DDD.Domain.Common.ValueObjects;

namespace DDD.Infrastructure.Converters;

public sealed class CustomerNameJsonConverter : JsonConverter<CustomerName>
{
    public override CustomerName Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException(
                $"Ожидался объект CustomerName, получен {reader.TokenType}.");

        string? value = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException(
                    "Ожидалось имя свойства CustomerName.");

            var propertyName = reader.GetString();

            if (!reader.Read())
                throw new JsonException(
                    "Неожиданный конец JSON при чтении CustomerName.");

            if (string.Equals(
                    propertyName,
                    "value",
                    StringComparison.OrdinalIgnoreCase))
            {
                value = reader.GetString();
            }
            else
            {
                reader.Skip();
            }
        }

        if (value is null)
            throw new JsonException(
                "CustomerName.Value отсутствует.");

        try
        {
            return new CustomerName(value);
        }
        catch (ArgumentException ex)
        {
            throw new JsonException(
                $"Некорректное имя клиента: '{value}'.",
                ex);
        }
    }

    public override void Write(
        Utf8JsonWriter writer,
        CustomerName value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("value", value.Value);
        writer.WriteEndObject();
    }
}