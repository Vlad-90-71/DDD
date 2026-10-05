namespace DDD.MessageBroker.Contracts;

public sealed record BrokerMessage(Guid EventId, string Type, string Content);
public sealed record BrokerDelivery(Guid DeliveryId, BrokerMessage Message);

public interface IMessagePublisher
{
    Task PublishAsync(BrokerMessage message, CancellationToken cancellationToken);
}