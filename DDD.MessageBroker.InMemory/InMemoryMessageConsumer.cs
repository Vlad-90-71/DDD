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
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delivery = await broker.ReceiveAsync(
                stoppingToken);

            try
            {
                var domainEvent = serializer.Deserialize(
                    delivery.Message.EventId,
                    delivery.Message.Type,
                    delivery.Message.Content);

                using var scope =
                    scopeFactory.CreateScope();

                var dispatcher =
                    scope.ServiceProvider
                        .GetRequiredService<IDomainEventDispatcher>();

                var result =
                    await dispatcher.DispatchAsync(
                        domainEvent,
                        stoppingToken);

                switch (result.Status)
                {
                    case EventProcessingClaimStatus.NoHandler:
                    case EventProcessingClaimStatus.AlreadyProcessed:
                    case EventProcessingClaimStatus.Failed:

                        broker.Ack(
                            delivery.DeliveryId);

                        break;

                    case EventProcessingClaimStatus.InProgress:

                        await Task.Delay(
                            TimeSpan.FromSeconds(1),
                            stoppingToken);

                        await broker.NackAsync(
                            delivery.DeliveryId,
                            requeue: true,
                            stoppingToken);

                        break;

                    case EventProcessingClaimStatus.Claimed:

                        var unitOfWork =
                            scope.ServiceProvider
                                .GetRequiredService<IUnitOfWork>();

                        await unitOfWork.SaveChangesAsync(
                            stoppingToken);

                        broker.Ack(
                            delivery.DeliveryId);

                        break;
                }
            }
            catch (Exception ex)
            {
                try
                {
                    var eventStore =
                        scopeFactory
                            .CreateScope()
                            .ServiceProvider
                            .GetRequiredService<IEventProcessingStore>();

                    // Здесь этот вариант нам не подходит,
                    // потому что claimToken должен быть из результата claim.
                }
                finally
                {
                    await broker.NackAsync(
                        delivery.DeliveryId,
                        requeue: true,
                        stoppingToken);
                }
            }
        }
    }
}