using DDD.Application.Common.Messaging;

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
}