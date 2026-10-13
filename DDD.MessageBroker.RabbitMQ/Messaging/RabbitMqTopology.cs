using DDD.MessageBroker.RabbitMQ.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DDD.MessageBroker.RabbitMQ.Messaging;

public sealed class RabbitMqTopology(IOptions<RabbitMqOptions> options)
{
    public async Task ConfigureAsync(IChannel channel, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        // Основной exchange.
        await channel.ExchangeDeclareAsync(
            exchange: settings.Exchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        // Exchange для сообщений, ожидающих повторной обработки.
        await channel.ExchangeDeclareAsync(
            exchange: settings.RetryExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        // Exchange для окончательно неудачных сообщений.
        await channel.ExchangeDeclareAsync(
            exchange: settings.DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: settings.InProgressExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        // Основная очередь.
        var mainQueueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = settings.RetryExchange,
            ["x-dead-letter-routing-key"] = settings.RetryRoutingKey
        };

        await channel.QueueDeclareAsync(
            queue: settings.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: mainQueueArguments,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: settings.Queue,
            exchange: settings.Exchange,
            routingKey: settings.RoutingKey,
            arguments: null,
            cancellationToken: cancellationToken);

        // Retry queue: по истечении TTL сообщение возвращается
        // в основной exchange и routing key.
        var retryArguments = new Dictionary<string, object?>
        {
            ["x-message-ttl"] = settings.RetryDelayMilliseconds,
            ["x-dead-letter-exchange"] = settings.Exchange,
            ["x-dead-letter-routing-key"] = settings.RoutingKey
        };

        await channel.QueueDeclareAsync(
            queue: settings.RetryQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: retryArguments,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: settings.RetryQueue,
            exchange: settings.RetryExchange,
            routingKey: settings.RetryRoutingKey,
            arguments: null,
            cancellationToken: cancellationToken);

        // DLQ хранит сообщения, которые больше не будем повторять.
        await channel.QueueDeclareAsync(
            queue: settings.DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: settings.DeadLetterQueue,
            exchange: settings.DeadLetterExchange,
            routingKey: settings.DeadLetterRoutingKey,
            arguments: null,
            cancellationToken: cancellationToken);

        var inProgressArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = settings.Exchange,
            ["x-dead-letter-routing-key"] = settings.RoutingKey
        };

        await channel.QueueDeclareAsync(
            queue: settings.InProgressQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: inProgressArguments,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: settings.InProgressQueue,
            exchange: settings.InProgressExchange,
            routingKey: settings.InProgressRoutingKey,
            cancellationToken: cancellationToken);
    }
}