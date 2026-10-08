using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DDD.Domain.Common.Events;
using DDD.Eventing.Contracts;
using DDD.MessageBroker.Contracts;

namespace DDD.MessageBroker.InMemory;

public sealed class InMemoryMessageConsumer(
    InMemoryMessageBroker broker,
    IDomainEventSerializer serializer,
    IServiceScopeFactory scopeFactory,
    ILogger<InMemoryMessageConsumer> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delivery = await broker.ReceiveAsync(stoppingToken);

            try
            {
                using var scope = scopeFactory.CreateScope();

                var domainEvent = serializer.Deserialize(
                    delivery.Message.EventId,
                    delivery.Message.Type,
                    delivery.Message.Content);

                logger.LogDebug(
                    "Domain event deserialized. DeliveryId={DeliveryId}, EventId={EventId}, Type={Type}",
                    delivery.DeliveryId, domainEvent.EventId, domainEvent.GetType().Name);

                var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
                var result = await dispatcher.DispatchAsync(domainEvent, stoppingToken);

                switch (result.Status)
                {
                    case EventProcessingClaimStatus.NoHandler:
                    case EventProcessingClaimStatus.AlreadyProcessed:
                    case EventProcessingClaimStatus.Failed:

                        logger.LogDebug(
                            "Broker delivery acknowledged. DeliveryId={DeliveryId}, EventId={EventId}, Status={Status}",
                            delivery.DeliveryId, domainEvent.EventId, result.Status);
                       
                        broker.Ack(delivery.DeliveryId);
                        break;

                    case EventProcessingClaimStatus.InProgress:

                        logger.LogDebug(
                            "Event is currently being processed. DeliveryId={DeliveryId}, EventId={EventId}. Requeueing after delay.",
                            delivery.DeliveryId, domainEvent.EventId);

                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                        await broker.NackAsync(delivery.DeliveryId, stoppingToken);
                        break;

                    case EventProcessingClaimStatus.Retry:

                        logger.LogDebug(
                            "Event will be retried. DeliveryId={DeliveryId}, EventId={EventId}",
                            delivery.DeliveryId, domainEvent.EventId);

                        await broker.NackAsync(delivery.DeliveryId, stoppingToken);
                        break;

                    case EventProcessingClaimStatus.Claimed:

                        await ProcessClaimedAsync(delivery, result, domainEvent, scope, stoppingToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Consumer failed to process delivery. DeliveryId={DeliveryId}, EventId={EventId}, Type={Type}",
                    delivery.DeliveryId, delivery.Message.EventId, delivery.Message.Type);

                await broker.NackAsync(delivery.DeliveryId, stoppingToken);
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
            logger.LogDebug(
                "Saving consumer transaction. DeliveryId={DeliveryId}, EventId={EventId}, Type={Type}",
                delivery.DeliveryId, result.EventId, domainEvent.GetType().Name);

            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogDebug(
                "Consumer transaction committed. DeliveryId={DeliveryId}, EventId={EventId}, Type={Type}",
                delivery.DeliveryId,  result.EventId, domainEvent.GetType().Name);

            broker.Ack(delivery.DeliveryId);

            logger.LogInformation(
                "Broker delivery acknowledged after successful processing. DeliveryId={DeliveryId}, EventId={EventId}, Type={Type}",
                delivery.DeliveryId, result.EventId, domainEvent.GetType().Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Consumer transaction failed. DeliveryId={DeliveryId}, EventId={EventId}, Type={Type}",
                delivery.DeliveryId, result.EventId, domainEvent.GetType().Name);

            var failed = await ReleaseClaimAsync(result.EventId, result.ClaimToken!.Value, ex, cancellationToken);

            if (failed)
            {
                broker.Ack(delivery.DeliveryId);

                logger.LogWarning(
                    "Delivery acknowledged because event processing reached the maximum retry count. DeliveryId={DeliveryId}, EventId={EventId}",
                    delivery.DeliveryId, result.EventId);
            }
            else
            {
                await broker.NackAsync(delivery.DeliveryId, cancellationToken);

                logger.LogDebug(
                    "Delivery requeued after consumer transaction failure. DeliveryId={DeliveryId}, EventId={EventId}",
                    delivery.DeliveryId, result.EventId);
            }
        }
    }

    private async Task<bool> ReleaseClaimAsync(Guid eventId, Guid claimToken, Exception exception, CancellationToken cancellationToken)
    {
        // Используем новый scope:
        // текущий DbContext мог получить ошибку SaveChangesAsync.
        using var scope = scopeFactory.CreateScope();

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventProcessingStore>();

        return await eventStore.ReleaseClaimAsync(eventId, claimToken, exception.ToString(), cancellationToken);
    }
}