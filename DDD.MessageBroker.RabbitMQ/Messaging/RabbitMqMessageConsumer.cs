using DDD.Domain.Common.Events;
using DDD.Eventing.Contracts;
using DDD.MessageBroker.RabbitMQ.Connection;
using DDD.MessageBroker.RabbitMQ.Configuration;
using DDD.MessageBroker.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DDD.MessageBroker.RabbitMQ.Messaging;

public sealed class RabbitMqMessageConsumer(
    RabbitMqConnectionManager connectionManager,
    RabbitMqTopology topology,
    IOptions<RabbitMqOptions> options,
    IDomainEventSerializer serializer,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqMessageConsumer> logger,
    IDomainEventMetrics metrics)
    : BackgroundService
{
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IChannel? channel = null;
            TaskCompletionSource channelClosed = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                var connection =
                    await connectionManager.GetConnectionAsync(stoppingToken);

                channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                _channel = channel;

                channel.ChannelShutdownAsync += (_, _) =>
                {
                    channelClosed.TrySetResult();
                    return Task.CompletedTask;
                };

                await topology.ConfigureAsync(channel, stoppingToken);

                await channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: 1,
                    global: false,
                    cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (_, delivery) =>
                {
                    try
                    {
                        await ProcessDeliveryAsync(delivery, stoppingToken);
                    }
                    catch (OperationCanceledException)
                        when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "Unhandled RabbitMQ delivery error. " +
                            "MessageId={MessageId}, DeliveryTag={DeliveryTag}",
                            delivery.BasicProperties.MessageId, delivery.DeliveryTag);

                        // Не используем delivery tag закрытого канала.
                        if (channel.IsOpen)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

                            if (channel.IsOpen)
                            {
                                await channel.BasicNackAsync(
                                    delivery.DeliveryTag,
                                    multiple: false,
                                    requeue: true,
                                    cancellationToken: stoppingToken);
                            }
                        }
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: options.Value.Queue,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                logger.LogInformation(
                    "RabbitMQ consumer started. Queue={Queue}",
                    options.Value.Queue);

                // Ждём закрытия канала либо остановки приложения.
                await Task.WhenAny(channelClosed.Task, Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken));

                stoppingToken.ThrowIfCancellationRequested();

                logger.LogWarning("RabbitMQ consumer channel closed. Recreating channel.");
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "RabbitMQ consumer startup or channel failure. Retrying in 5 seconds.");
            }
            finally
            {
                if (channel is not null)
                {
                    try
                    {
                        await channel.DisposeAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, "Error disposing RabbitMQ consumer channel.");
                    }
                }

                if (ReferenceEquals(_channel, channel))
                    _channel = null;
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
    private async Task ProcessDeliveryAsync(BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
    {
        BrokerMessage? message;

        // 1. Десериализуем транспортное сообщение.
        try
        {
            message = System.Text.Json.JsonSerializer.Deserialize<BrokerMessage>(delivery.Body.Span);

            if (message is null ||
                message.EventId == Guid.Empty ||
                string.IsNullOrWhiteSpace(message.Type) ||
                string.IsNullOrWhiteSpace(message.Content))
            {
                throw new System.Text.Json.JsonException("RabbitMQ message has an invalid envelope.");
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogError(ex, "Invalid RabbitMQ message envelope. Sending to DLQ.");

            await MoveToDeadLetterAsync(delivery, cancellationToken);
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();

            // 2. Десериализуем доменное событие.
            DomainEvent domainEvent;

            try
            {
                domainEvent = serializer.Deserialize(message.EventId, message.Type, message.Content);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError( ex,
                    "Domain event deserialization failed. EventId={EventId}, Type={Type}. Sending to DLQ.",
                    message.EventId, message.Type);

                await MoveToDeadLetterAsync(delivery, cancellationToken);
                return;
            }

            // 3. Диспетчеризация.
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
            var result = await dispatcher.DispatchAsync(domainEvent, cancellationToken);

            switch (result.Status)
            {
                case EventProcessingClaimStatus.AlreadyProcessed:

                    await AckAsync(delivery, cancellationToken);

                    logger.LogInformation(
                        "Duplicate event acknowledged. EventId={EventId}",
                        message.EventId);
                    break;

                case EventProcessingClaimStatus.NoHandler:

                    logger.LogWarning(
                        "Event has no registered handler. EventId={EventId}, Type={Type}",
                        message.EventId, message.Type);

                    await AckAsync(delivery, cancellationToken);
                    break;

                case EventProcessingClaimStatus.Failed:

                    // Окончательный отказ: сохраняем сообщение для анализа.
                    await MoveToDeadLetterAsync(delivery, cancellationToken);

                    logger.LogWarning(
                        "Failed event moved to DLQ. EventId={EventId}",
                        message.EventId);
                    break;

                case EventProcessingClaimStatus.InProgress:

                    logger.LogDebug(
                        "Event is already being processed. EventId={EventId}",
                        message.EventId);

                    await DelayInProgressAsync(delivery, result.ClaimedUntilUtc, cancellationToken);
                    break;

                case EventProcessingClaimStatus.Retry:

                    logger.LogDebug(
                        "Event will be retried. EventId={EventId}",
                        message.EventId);

                    await RetryOrDeadLetterAsync(delivery, cancellationToken);
                    break;

                case EventProcessingClaimStatus.Claimed:

                    await ProcessClaimedAsync(delivery, result, domainEvent, scope, cancellationToken);
                    break;

                default:

                    logger.LogError(
                        "Unexpected event processing status. EventId={EventId}, Status={Status}. Sending to DLQ.",
                        message.EventId, result.Status);

                    await MoveToDeadLetterAsync(delivery, cancellationToken);
                    break;
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Не подтверждаем доставку при остановке.
            // RabbitMQ повторно доставит неподтверждённое сообщение.
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "RabbitMQ consumer failed. EventId={EventId}",
                message.EventId);

            // Ошибки инфраструктуры могут быть временными.
            // Отправляем в Retry Queue, а не делаем NACK + requeue.
            await RetryOrDeadLetterAsync(delivery, cancellationToken);
        }
    }
    
    private async Task ProcessClaimedAsync(
        BasicDeliverEventArgs delivery,
        EventProcessingClaimResult result,
        DomainEvent domainEvent,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        try
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            metrics.EventProcessed();

            await AckAsync(delivery, cancellationToken);

            logger.LogDebug(
                "RabbitMQ delivery acknowledged after successful processing. EventId={EventId}, Type={Type}",
                result.EventId, domainEvent.GetType().Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Consumer transaction failed. EventId={EventId}, Type={Type}",
                result.EventId, domainEvent.GetType().Name);

            metrics.EventFailed();

            var failed = await ReleaseClaimAsync(result.EventId, result.ClaimToken!.Value, ex, cancellationToken);

            if (failed)
            {
                await AckAsync(delivery, cancellationToken);

                logger.LogWarning(
                    "Delivery acknowledged because the retry limit was reached. EventId={EventId}",
                    result.EventId);
            }
            else
                await RetryOrDeadLetterAsync(delivery, cancellationToken);
        }
    }

    private async Task MarkEventAsFailedIfKnownAsync(BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
    {
        BrokerMessage? message;

        try
        {
            message = System.Text.Json.JsonSerializer
                .Deserialize<BrokerMessage>(delivery.Body.Span);
        }
        catch (System.Text.Json.JsonException)
        {
            // Некорректный конверт: достоверно извлечь EventId невозможно.
            logger.LogWarning(
                "Cannot synchronize EventProcessingState because the broker message " +
                "envelope is invalid. MessageId={MessageId}",
                delivery.BasicProperties.MessageId);

            return;
        }

        if (message is null || message.EventId == Guid.Empty)
        {
            logger.LogWarning(
                "Cannot synchronize EventProcessingState because EventId is missing. MessageId={MessageId}",
                delivery.BasicProperties.MessageId);

            return;
        }

        var error =
            $"Message moved to RabbitMQ DLQ. " +
            $"RetryCount={GetRetryCount(delivery)}, " +
            $"RoutingKey={delivery.RoutingKey}.";

        // Отдельный scope — отдельный AppDbContext.
        using var scope = scopeFactory.CreateScope();

        var eventProcessingStore =
            scope.ServiceProvider.GetRequiredService<IEventProcessingStore>();

        await eventProcessingStore.MarkAsFailedAsync(message.EventId, error, cancellationToken);
    }
    private async Task MoveToDeadLetterAsync(BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        // 1. Сначала публикуем сообщение в DLQ.
        await PublishRoutedCopyAsync(
            delivery,
            settings.DeadLetterExchange,
            settings.DeadLetterRoutingKey,
            GetRetryCount(delivery),
            cancellationToken);

        // 2. Если EventId удалось прочитать, фиксируем окончательный отказ в БД.
        await MarkEventAsFailedIfKnownAsync(delivery, cancellationToken);

        // 3. Подтверждаем исходное сообщение только после успешного шага 2.
        await AckAsync(delivery, cancellationToken);

        logger.LogWarning(
            "Message moved to DLQ. MessageId={MessageId}, RoutingKey={RoutingKey}",
            delivery.BasicProperties.MessageId, delivery.RoutingKey);
    }
    private async Task DelayInProgressAsync(
        BasicDeliverEventArgs delivery,
        DateTime? claimedUntilUtc,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;

        var now = DateTime.UtcNow;

        // Если срок claim неизвестен или уже истёк,
        // не задерживаем сообщение надолго.
        var delay = claimedUntilUtc.HasValue
            ? claimedUntilUtc.Value - now + TimeSpan.FromMilliseconds(settings.InProgressDelayBufferMilliseconds)
            : TimeSpan.FromMilliseconds(1000);

        if (delay < TimeSpan.FromMilliseconds(1000))
            delay = TimeSpan.FromMilliseconds(1000);

        var delayMilliseconds = (long)Math.Ceiling(delay.TotalMilliseconds);

        // Используем отдельную очередь InProgress.
        await PublishRoutedCopyAsync(
            delivery,
            settings.InProgressExchange,
            settings.InProgressRoutingKey,
            GetRetryCount(delivery),
            cancellationToken,
            expirationMilliseconds: delayMilliseconds);

        // ACK исходного сообщения только после успешной публикации копии.
        await AckAsync(delivery, cancellationToken);

        logger.LogInformation(
            "Message delayed because event is already claimed. " +
            "MessageId={MessageId}, ClaimedUntilUtc={ClaimedUntilUtc}, DelayMs={DelayMs}",
            delivery.BasicProperties.MessageId, claimedUntilUtc, delayMilliseconds);
    }
    private async Task<bool> ReleaseClaimAsync(
        Guid eventId,
        Guid claimToken,
        Exception exception,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var eventStore = scope.ServiceProvider.GetRequiredService<IEventProcessingStore>();

        return await eventStore.ReleaseClaimAsync(eventId, claimToken, exception.ToString(), cancellationToken);
    }

    private const string RetryHeader = "x-ddd-retry-count";

    private int GetRetryCount(BasicDeliverEventArgs delivery)
    {
        var headers = delivery.BasicProperties.Headers;

        if (headers is null ||
            !headers.TryGetValue("x-death", out var raw) ||
            raw is not System.Collections.IEnumerable deaths)
        {
            return 0;
        }

        long total = 0;
        var mainQueue = options.Value.Queue;

        foreach (var item in deaths)
        {
            if (item is not System.Collections.IDictionary death)
                continue;

            var queue = HeaderString(death["queue"]);
            var reason = HeaderString(death["reason"]);

            if (!string.Equals(queue, mainQueue, StringComparison.Ordinal) ||
                !string.Equals(reason, "rejected", StringComparison.Ordinal))
            {
                continue;
            }

            if (TryGetCount(death["count"], out var count))
                total = Math.Min(total + count, int.MaxValue);
        }

        return (int)total;
    }

    private static string HeaderString(object? value) =>
        value switch
        {
            byte[] bytes => System.Text.Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => value?.ToString() ?? string.Empty
        };

    private static bool TryGetCount(object? value, out long count)
    {
        try
        {
            count = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);

            return count >= 0;
        }
        catch (Exception ex) when (
            ex is FormatException or InvalidCastException or OverflowException)
        {
            count = 0;
            return false;
        }
    }

    private async Task PublishRoutedCopyAsync(
        BasicDeliverEventArgs delivery,
        string exchange,
        string routingKey,
        int retryCount,
        CancellationToken cancellationToken,
        long? expirationMilliseconds = null)
    {
        var connection = await connectionManager.GetConnectionAsync(cancellationToken);

        // Отдельный канал: consumer channel не используем
        // одновременно для публикации и ACK/NACK.
        await using var publishChannel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            cancellationToken);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = delivery.BasicProperties.ContentType,
            MessageId = delivery.BasicProperties.MessageId,
            Type = delivery.BasicProperties.Type,
            Headers = new Dictionary<string, object?> { [RetryHeader] = retryCount },
            Expiration = expirationMilliseconds?.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        await publishChannel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: properties,
            body: delivery.Body,
            cancellationToken: cancellationToken);
    }

    private async Task RetryOrDeadLetterAsync(BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
    {
        var retryCount = GetRetryCount(delivery);
        var settings = options.Value;

        logger.LogWarning(
            "RabbitMQ retry counter inspected. MessageId={MessageId}, " +
            "RetryCount={RetryCount}, MaxRetryCount={MaxRetryCount}, Headers={Headers}",
            delivery.BasicProperties.MessageId, retryCount, settings.MaxRetryCount,
            delivery.BasicProperties.Headers is null
                ? "<null>"
                : string.Join(", ", delivery.BasicProperties.Headers.Select(
                    header => $"{header.Key}={header.Value?.GetType().FullName}")));

        if (retryCount >= settings.MaxRetryCount)
        {
            await MoveToDeadLetterAsync(delivery, cancellationToken);

            logger.LogError(
                "Message moved to DLQ after exhausting retries. " +
                "RetryCount={RetryCount}, MessageId={MessageId}",
                retryCount, delivery.BasicProperties.MessageId);

            return;
        }

        await NackAsync(delivery, requeue: false, cancellationToken);

        logger.LogWarning(
            "Message rejected for retry through RabbitMQ DLX. " +
            "PreviousRetries={RetryCount}, MaxRetry={MaxRetry}, MessageId={MessageId}",
            retryCount, settings.MaxRetryCount, delivery.BasicProperties.MessageId);
    }

    private async Task AckAsync(BasicDeliverEventArgs delivery, CancellationToken cancellationToken) =>
        await _channel!.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);

    private async Task NackAsync(BasicDeliverEventArgs delivery, bool requeue, CancellationToken cancellationToken) =>
        await _channel!.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: requeue, cancellationToken: cancellationToken);
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel is not null)
            await _channel.DisposeAsync();
    }
}