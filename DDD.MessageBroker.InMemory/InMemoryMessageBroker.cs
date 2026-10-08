using System.Collections.Concurrent;
using System.Threading.Channels;
using DDD.MessageBroker.Contracts;

namespace DDD.MessageBroker.InMemory;

public sealed class InMemoryMessageBroker
{
    private readonly Channel<BrokerDelivery> _channel =
        Channel.CreateUnbounded<BrokerDelivery>();

    private readonly ConcurrentDictionary<Guid, BrokerDelivery> _pending = [];

    public Task PublishAsync(BrokerMessage message, CancellationToken cancellationToken)
    {
        var delivery = new BrokerDelivery(Guid.NewGuid(), message);

        return _channel.Writer.WriteAsync(delivery, cancellationToken).AsTask();
    }

    public async ValueTask<BrokerDelivery> ReceiveAsync(CancellationToken cancellationToken)
    {
        var delivery = await _channel.Reader.ReadAsync(cancellationToken);

        _pending[delivery.DeliveryId] = delivery;

        return delivery;
    }

    public void Ack(Guid deliveryId)
    {
        _pending.TryRemove(deliveryId, out _);
    }

    public async Task NackAsync(Guid deliveryId, /*bool requeue,*/ CancellationToken cancellationToken)
    {
        if (!_pending.TryRemove(deliveryId, out var delivery))
            return;

        await _channel.Writer.WriteAsync(delivery, cancellationToken);
    }
}