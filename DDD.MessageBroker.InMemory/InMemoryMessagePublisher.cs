using DDD.MessageBroker.Contracts;

namespace DDD.MessageBroker.InMemory;

public sealed class InMemoryMessagePublisher(InMemoryMessageBroker broker) : IMessagePublisher
{
    public Task PublishAsync(BrokerMessage message, CancellationToken cancellationToken) =>
        broker.PublishAsync(message, cancellationToken);
}