using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using DDD.MessageBroker.Contracts;
using DDD.MessageBroker.RabbitMQ.Configuration;
using DDD.MessageBroker.RabbitMQ.Connection;

namespace DDD.MessageBroker.RabbitMQ.Messaging;

public sealed class RabbitMqMessagePublisher(
    RabbitMqConnectionManager connectionManager,
    RabbitMqTopology topology,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqMessagePublisher> logger)
    : IMessagePublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim _publishLock = new(1, 1);

    private IChannel? _channel;

    public async Task PublishAsync(BrokerMessage message, CancellationToken cancellationToken)
    {
        await _publishLock.WaitAsync(cancellationToken);

        try
        {
            var channel = await GetChannelAsync(cancellationToken);
            var settings = options.Value;

            var body = JsonSerializer.SerializeToUtf8Bytes(message);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = message.EventId.ToString(),
                Type = message.Type
            };

            await channel.BasicPublishAsync(
                exchange: settings.Exchange,
                routingKey: settings.RoutingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            logger.LogDebug(
                "RabbitMQ message published. EventId={EventId}, Type={Type}",
                message.EventId, message.Type);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "RabbitMQ message publishing failed. EventId={EventId}, Type={Type}",
                message.EventId, message.Type);

            throw;
        }
        finally
        {
            _publishLock.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        var connection = await connectionManager.GetConnectionAsync(cancellationToken);

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        _channel = await connection.CreateChannelAsync(channelOptions, cancellationToken);

        await topology.ConfigureAsync(_channel, cancellationToken);

        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        _publishLock.Dispose();
    }
}