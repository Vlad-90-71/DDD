using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using DDD.Eventing.Contracts;

namespace DDD.MessageBroker.InMemory;

public sealed class InMemoryMessageConsumer(
    InMemoryMessageBroker broker,
    IDomainEventSerializer serializer,
    IServiceScopeFactory scopeFactory)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delivery = await broker.ReceiveAsync(stoppingToken);

            try
            {
                var domainEvent = serializer.Deserialize(
                    delivery.Message.EventId,
                    delivery.Message.Type,
                    delivery.Message.Content);

                using var scope = scopeFactory.CreateScope();

                var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

                await dispatcher.DispatchAsync(domainEvent, stoppingToken);

                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                await unitOfWork.SaveChangesAsync(stoppingToken);

                broker.Ack(delivery.DeliveryId);
            }
            catch
            {
                await broker.NackAsync(delivery.DeliveryId, requeue: true, stoppingToken);
            }
        }
    }
}