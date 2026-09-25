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
    private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Web;

    public OutboxMessage Serialize(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        Type type = domainEvent.GetType();

        string typeName = type.AssemblyQualifiedName
            ?? throw new InvalidOperationException($"Не удалось получить имя сборки для типа {type.Name}");

        string content = JsonSerializer.Serialize(domainEvent, type, JsonOptions);

        return new OutboxMessage(
            domainEvent.EventId,
            typeName,
            content,
            domainEvent.OccurredOnUtc);
    }

    public IDomainEvent Deserialize(OutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        Type type = Type.GetType(message.Type)
            ?? throw new InvalidOperationException(
                $"Не найден тип Domain Event: {message.Type}");

        var deserialized = JsonSerializer.Deserialize(message.Content, type, JsonOptions);

        if (deserialized is not IDomainEvent domainEvent)
            throw new InvalidOperationException(
                $"Десериализованный объект не реализует интерфейс IDomainEvent. Тип: {message.Type}");

        return domainEvent;
    }
}
