using DDD.Eventing.Contracts;

namespace DDD.MessageBroker.InMemory;

public sealed class InMemoryMessageConsumer(
    InMemoryMessageBroker broker,
    IDomainEventSerializer serializer,
    IDomainEventDispatcher dispatcher)
{
    public async Task ConsumeAsync(
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var delivery = await broker.ReceiveAsync(
                cancellationToken);

            try
            {
                var domainEvent = serializer.Deserialize(
                    delivery.Message.Type,
                    delivery.Message.Content);

                await dispatcher.DispatchAsync(
                    domainEvent,
                    cancellationToken);

                await broker.AckAsync(
                    delivery.DeliveryId,
                    cancellationToken);
            }
            catch
            {
                await broker.NackAsync(
                    delivery.DeliveryId,
                    requeue: true,
                    cancellationToken);

                // Не проглатываем ошибку.
                // Но Consumer продолжает работать.
            }
        }
    }
}