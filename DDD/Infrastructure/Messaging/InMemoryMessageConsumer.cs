using DDD.Application.Common.Events;
using DDD.Application.Common.Messaging;
using DDD.Domain.Common.Events;
using DDD.Infrastructure.Outbox;

namespace DDD.Infrastructure.Messaging;

public sealed class InMemoryMessageConsumer(
    InMemoryMessageBroker broker,
    IOutboxMessageSerializer serializer,
    IDomainEventDispatcher dispatcher,
    IServiceScopeFactory scopeFactory)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await foreach (var message in broker.ConsumeAsync(stoppingToken))
        {
            await ProcessMessageAsync(
                message,
                stoppingToken);
        }
    }

    private async Task ProcessMessageAsync(
        BrokerMessage message,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var scopedSerializer =
            scope.ServiceProvider
                .GetRequiredService<IOutboxMessageSerializer>();

        var scopedDispatcher =
            scope.ServiceProvider
                .GetRequiredService<IDomainEventDispatcher>();

        var domainEvent = Deserialize(
            message,
            scopedSerializer);

        await scopedDispatcher.DispatchAsync(
            domainEvent,
            cancellationToken);
    }

    private static IDomainEvent Deserialize(
        BrokerMessage message,
        IOutboxMessageSerializer serializer)
    {
        var outboxMessage = new OutboxMessageForDeserialization(
            message.EventId,
            message.Type,
            message.Content);

        return serializer.Deserialize(outboxMessage);
    }
}