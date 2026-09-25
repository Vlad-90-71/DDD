using System.Text.Json;
using DDD.Domain.Common.Events;
using DDD.Eventing.Contracts;

namespace DDD.Infrastructure.Events;

public sealed class DomainEventSerializer : IDomainEventSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Web;

    public string Serialize(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions);
    }

    public IDomainEvent Deserialize(string typeName, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Type type = Type.GetType(typeName)
            ?? throw new InvalidOperationException($"Не найден тип Domain Event: {typeName}");

        var deserialized = JsonSerializer.Deserialize(content, type, JsonOptions);

        if (deserialized is not IDomainEvent domainEvent)
            throw new InvalidOperationException(
                $"Десериализованный объект не реализует интерфейс IDomainEvent. Тип: {typeName}");

        return domainEvent;
    }
}
