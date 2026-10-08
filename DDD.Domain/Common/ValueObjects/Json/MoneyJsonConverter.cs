using System.Text.Json;
using System.Text.Json.Serialization;
using DDD.Domain.Common.ValueObjects;

public sealed class MoneyJsonConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Ожидался объект Money, получен токен {reader.TokenType}.");

        decimal? amount = null;
        string? currency = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Ожидалось имя свойства Money.");

            var propertyName = reader.GetString();

            if (!reader.Read())
                throw new JsonException("Неожиданный конец JSON при чтении Money.");

            switch (propertyName)
            {
                case "amount": amount = reader.GetDecimal(); break;
                case "currency": currency = reader.GetString(); break;
                default: reader.Skip(); break;
            }
        }

        if (amount is null)
            throw new JsonException("Money.Amount отсутствует.");

        if (currency is null)
            throw new JsonException("Money.Currency отсутствует.");

        try
        {
            return new Money(amount.Value, currency);
        }
        catch (ArgumentException ex)
        {
            throw new JsonException($"Некорректное значение Money: {amount} {currency}.", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("amount", value.Amount);
        writer.WriteString("currency", value.Currency);
        writer.WriteEndObject();
    }
}