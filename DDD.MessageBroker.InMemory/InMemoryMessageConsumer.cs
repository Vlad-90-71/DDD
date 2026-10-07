using DDD.Application.Common;
using DDD.Domain.Common.Events;
using DDD.Eventing.Contracts;
using DDD.MessageBroker.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

            var scope = scopeFactory.CreateScope();

            try
            {
                var domainEvent = serializer.Deserialize(
                    delivery.Message.EventId,
                    delivery.Message.Type,
                    delivery.Message.Content);

                var dispatcher = scope.ServiceProvider
                    .GetRequiredService<IDomainEventDispatcher>();

                var result = await dispatcher.DispatchAsync(domainEvent, stoppingToken);

                Console.WriteLine(
                    $"Consumer: EventId={domainEvent.EventId}, " +
                    $"Type={domainEvent.GetType().Name}, " +
                    $"Status={result.Status}");

                switch (result.Status)
                {
                    case EventProcessingClaimStatus.NoHandler:
                    case EventProcessingClaimStatus.AlreadyProcessed:
                    case EventProcessingClaimStatus.Failed:

                        broker.Ack(delivery.DeliveryId);
                        break;

                    case EventProcessingClaimStatus.InProgress:

                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                        await broker.NackAsync(delivery.DeliveryId, /*requeue: true, */stoppingToken);
                        break;

                    case EventProcessingClaimStatus.Retry:

                        await broker.NackAsync(delivery.DeliveryId, /*requeue: true, */stoppingToken);
                        break;

                    case EventProcessingClaimStatus.Claimed:

                        await ProcessClaimedAsync(delivery, result, domainEvent, scope, stoppingToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
    $"Consumer ERROR: " +
    $"DeliveryId={delivery.DeliveryId}, " +
    $"EventId={delivery.Message.EventId}, " +
    $"Type={delivery.Message.Type}");

                Console.WriteLine(ex);

                await broker.NackAsync(delivery.DeliveryId, /*requeue: true,*/ stoppingToken);
            }
            finally
            {
                scope.Dispose();
            }
        }
    }

    private async Task ProcessClaimedAsync(
        BrokerDelivery delivery,
        EventProcessingClaimResult result,
        IDomainEvent domainEvent,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        try
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            broker.Ack(delivery.DeliveryId);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Consumer SaveChanges ERROR: " +
                $"EventId={result.EventId}, " +
                $"Type={domainEvent.GetType().Name}, " +
                $"Error={ex.Message}");

            var failed = await ReleaseClaimAsync(result.EventId, result.ClaimToken!.Value, ex, cancellationToken);

            if (failed)
                broker.Ack(delivery.DeliveryId);
            else
                await broker.NackAsync(delivery.DeliveryId, /*requeue: true,*/ cancellationToken);
        }
    }

    private async Task<bool> ReleaseClaimAsync(Guid eventId, Guid claimToken, Exception exception, CancellationToken cancellationToken)
    {
        // Используем новый scope:
        // текущий DbContext мог получить ошибку SaveChangesAsync.
        using var scope = scopeFactory.CreateScope();

        var eventStore = scope.ServiceProvider
            .GetRequiredService<IEventProcessingStore>();

        return await eventStore.ReleaseClaimAsync(eventId, claimToken, exception.ToString(), cancellationToken);
    }
}