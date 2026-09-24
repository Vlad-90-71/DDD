using System.Text.Json;
using DDD.Domain.Common.Events;

namespace DDD.Infrastructure.Outbox;

public interface IOutboxMessageSerializer
{
    OutboxMessage Serialize(IDomainEvent domainEvent);
    IDomainEvent Deserialize(OutboxMessage message);
}

public class OutboxMessageSerializer : IOutboxMessageSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OutboxMessage Serialize(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var type = domainEvent.GetType();

        var content = JsonSerializer.Serialize(domainEvent, type, JsonOptions);

        return new OutboxMessage(
            domainEvent.EventId,
            type.AssemblyQualifiedName!,
            content,
            domainEvent.OccurredOnUtc);
    }

    public IDomainEvent Deserialize(OutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var type = Type.GetType(message.Type)
            ?? throw new InvalidOperationException(
                $"Не найден тип Domain Event: {message.Type}");

        return (IDomainEvent)(JsonSerializer.Deserialize(message.Content, type, JsonOptions)
            ?? throw new InvalidOperationException(
                $"Не удалось десериализовать Domain Event: {message.Type}"));
    }
}