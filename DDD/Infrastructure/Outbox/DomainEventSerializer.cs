using System.Text.Json;
using DDD.Application.Common.Events;
using DDD.Domain.Common.Events;

namespace DDD.Infrastructure.Outbox;

public sealed class DomainEventSerializer : IDomainEventSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Serialize(IDomainEvent domainEvent)
    {
        var type = domainEvent.GetType();

        return JsonSerializer.Serialize(
            domainEvent,
            type,
            JsonOptions);
    }

    public IDomainEvent Deserialize(
        string typeName,
        string content)
    {
        var type = Type.GetType(typeName)
            ?? throw new InvalidOperationException(
                $"Не найден тип Domain Event: {typeName}");

        return (IDomainEvent)(
            JsonSerializer.Deserialize(
                content,
                type,
                JsonOptions)
            ?? throw new InvalidOperationException(
                $"Не удалось десериализовать Domain Event: {typeName}"));
    }
}