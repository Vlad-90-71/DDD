namespace DDD.Application.Common.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync(
        BrokerMessage message,
        CancellationToken cancellationToken);
}