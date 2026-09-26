using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DDD.Eventing.Contracts;

namespace DDD.MessageBroker.InMemory;

public sealed class InMemoryMessageConsumer(
    InMemoryMessageBroker broker,
    IDomainEventSerializer serializer,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delivery = await broker.ReceiveAsync(stoppingToken);

            try
            {
                var domainEvent = serializer.Deserialize(delivery.Message.Type, delivery.Message.Content);

                using var scope = scopeFactory.CreateScope();

                var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

                await dispatcher.DispatchAsync(domainEvent, stoppingToken);

                broker.AckAsync(delivery.DeliveryId);
            }
            catch
            {
                await broker.NackAsync(delivery.DeliveryId, requeue: true, stoppingToken);
            }
        }
    }
}