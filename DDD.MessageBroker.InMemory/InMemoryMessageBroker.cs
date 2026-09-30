using System.Collections.Concurrent;
using System.Threading.Channels;
using DDD.MessageBroker.Contracts;

namespace DDD.MessageBroker.InMemory;

public sealed class InMemoryMessageBroker
{
    public bool RequeueOnNack { get; set; } = true;

    private readonly Channel<BrokerDelivery> _channel =
        Channel.CreateUnbounded<BrokerDelivery>();

    private readonly ConcurrentDictionary<Guid, BrokerDelivery> _pending = [];

    private readonly TaskCompletionSource<bool> _nacked =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int _ackCount;
    private int _nackCount;

    public int AckCount =>
        Volatile.Read(ref _ackCount);

    public int NackCount =>
        Volatile.Read(ref _nackCount);

    public Task PublishAsync(
        BrokerMessage message,
        CancellationToken cancellationToken)
    {
        var delivery =
            new BrokerDelivery(
                Guid.NewGuid(),
                message);

        return _channel.Writer
            .WriteAsync(
                delivery,
                cancellationToken)
            .AsTask();
    }

    public async ValueTask<BrokerDelivery> ReceiveAsync(
        CancellationToken cancellationToken)
    {
        var delivery =
            await _channel.Reader.ReadAsync(
                cancellationToken);

        _pending[delivery.DeliveryId] =
            delivery;

        return delivery;
    }

    public void Ack(Guid deliveryId)
    {
        if (_pending.TryRemove(
                deliveryId,
                out _))
        {
            Interlocked.Increment(
                ref _ackCount);
        }
    }

    public async Task NackAsync(
        Guid deliveryId,
        bool requeue,
        CancellationToken cancellationToken)
    {
        if (!_pending.TryRemove(
                deliveryId,
                out var delivery))
        {
            return;
        }

        Interlocked.Increment(
            ref _nackCount);

        _nacked.TrySetResult(true);

        if (!requeue || !RequeueOnNack)
        {
            return;
        }

        await _channel.Writer.WriteAsync(
            delivery,
            cancellationToken);
    }

    public Task WaitUntilNackedAsync()
    {
        return _nacked.Task.WaitAsync(
            TimeSpan.FromSeconds(5));
    }
}