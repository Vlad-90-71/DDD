using System.Text.Json;
using System.Text.Json.Serialization;
using DDD.Domain.Common;

namespace DDD.Presentation.Common.Json;

public class SmartEnumJsonConverter<T> : JsonConverter<T> where T : SmartEnum<T>, ISmartEnum<T>
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int value))
        {
            return T.FromValue(value);
        }

        throw new JsonException($"Ожидалось число (int) для перечисления {typeToConvert.Name}.");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteNumberValue(value.Value);
    }
}
