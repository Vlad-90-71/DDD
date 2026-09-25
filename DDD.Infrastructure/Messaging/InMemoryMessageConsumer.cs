using DDD.Domain.Common.Events;
using DDD.Eventing.Contracts;
using DDD.Infrastructure.Outbox;
using DDD.MessageBroker.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        var outboxMessage = new OutboxMessage(
            message.EventId,
            message.Type,
            message.Content,
            DateTime.UtcNow);

        return serializer.Deserialize(outboxMessage);
    }
}