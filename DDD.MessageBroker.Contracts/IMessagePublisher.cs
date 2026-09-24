namespace DDD.MessageBroker.Contracts;

public interface IMessagePublisher
{
    Task PublishAsync(
        BrokerMessage message,
        CancellationToken cancellationToken);
}