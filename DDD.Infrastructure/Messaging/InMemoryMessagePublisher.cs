using DDD.MessageBroker.Contracts;

namespace DDD.Infrastructure.Messaging;

public sealed class InMemoryMessagePublisher(
    InMemoryMessageBroker broker) : IMessagePublisher
{
    public ValueTask PublishAsync(
        BrokerMessage message,
        CancellationToken cancellationToken)
    {
        return broker.PublishAsync(
            message,
            cancellationToken);
    }

    Task IMessagePublisher.PublishAsync(BrokerMessage message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}