using DDD.MessageBroker.Contracts;
using System.Threading.Channels;

namespace DDD.Infrastructure.Messaging;

public sealed class InMemoryMessageBroker
{
    private readonly Channel<BrokerMessage> _channel =
        Channel.CreateUnbounded<BrokerMessage>();

    public ValueTask PublishAsync(
        BrokerMessage message,
        CancellationToken cancellationToken)
    {
        return _channel.Writer.WriteAsync(
            message,
            cancellationToken);
    }

    public IAsyncEnumerable<BrokerMessage> ConsumeAsync(
        CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}