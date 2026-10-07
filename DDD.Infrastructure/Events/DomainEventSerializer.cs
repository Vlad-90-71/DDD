using System.Text.Json;
using DDD.Domain.Common.Events;
using DDD.Eventing.Contracts;
using DDD.Infrastructure.Converters;

namespace DDD.Infrastructure.Events;

public sealed class DomainEventSerializer : IDomainEventSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        options.Converters.Add(new EmailJsonConverter());
        options.Converters.Add(new CustomerNameJsonConverter());
        options.Converters.Add(new MoneyJsonConverter());

        return options;
    }

    public string Serialize(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions);
    }

    public IDomainEvent Deserialize(Guid eventId, string typeName, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var type = Type.GetType(typeName)
            ?? throw new InvalidOperationException(
                $"Не найден тип Domain Event: {typeName}");

        var deserialized = JsonSerializer.Deserialize(content, type, JsonOptions);

        if (deserialized is not IDomainEvent domainEvent)
            throw new InvalidOperationException(
                $"Десериализованный объект не реализует интерфейс IDomainEvent. Тип: {typeName}");

        if (domainEvent.EventId != eventId)
            throw new InvalidOperationException(
                $"EventId не совпадает. Ожидался: {eventId}, получен: {domainEvent.EventId}.");

        return domainEvent;
    }
}