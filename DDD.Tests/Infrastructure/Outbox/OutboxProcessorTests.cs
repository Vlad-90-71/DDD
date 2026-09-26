using DDD.Infrastructure;
using DDD.Infrastructure.Outbox;
using DDD.MessageBroker.Contracts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DDD.MessageBroker.Contracts;

namespace DDD.Tests.Infrastructure.Outbox;

public sealed class OutboxProcessorTests
{
    [Fact]
    public async Task ProcessAsync_ShouldPublishMessageAndMarkAsProcessed()
    {
        // Arrange
        await using var connection = CreateConnection();

        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var eventId = Guid.NewGuid();

        var message = new OutboxMessage(
            eventId,
            "OrderCanceledEvent",
            """{"orderId":123}""",
            DateTime.UtcNow);

        context.OutboxMessages.Add(message);

        await context.SaveChangesAsync();

        var publisher = new TestMessagePublisher();

        var processor = new OutboxProcessor(
            context,
            publisher);

        // Act
        await processor.ProcessAsync(
            CancellationToken.None);

        // Assert
        var processedMessage = await context.OutboxMessages
            .SingleAsync();

        Assert.NotNull(
            processedMessage.ProcessedOnUtc);

        Assert.Null(
            processedMessage.Error);

        Assert.Null(
            processedMessage.NextAttemptOnUtc);

        Assert.Null(
            processedMessage.ClaimToken);

        Assert.Null(
            processedMessage.ClaimedUntilUtc);

        Assert.NotNull(
            publisher.PublishedMessage);

        Assert.Equal(
            eventId,
            publisher.PublishedMessage.EventId);

        Assert.Equal(
            "OrderCanceledEvent",
            publisher.PublishedMessage.Type);

        Assert.Equal(
            """{"orderId":123}""",
            publisher.PublishedMessage.Content);
    }

    [Fact]
    public async Task ProcessAsync_ShouldScheduleRetry_WhenPublisherThrows()
    {
        // Arrange
        await using var connection = CreateConnection();

        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var message = new OutboxMessage(
            Guid.NewGuid(),
            "OrderCanceledEvent",
            """{"orderId":123}""",
            DateTime.UtcNow);

        context.OutboxMessages.Add(message);

        await context.SaveChangesAsync();

        var publisher = new FailingMessagePublisher(
            "Ошибка публикации сообщения.");

        var processor = new OutboxProcessor(
            context,
            publisher);

        // Act
        await processor.ProcessAsync(
            CancellationToken.None);

        // Assert
        var failedMessage = await context.OutboxMessages
            .SingleAsync();

        Assert.Null(
            failedMessage.ProcessedOnUtc);

        Assert.Null(
            failedMessage.FailedOnUtc);

        Assert.Equal(
            1,
            failedMessage.RetryCount);

        Assert.Equal(
            "Ошибка публикации сообщения.",
            failedMessage.Error);

        Assert.NotNull(
            failedMessage.NextAttemptOnUtc);

        Assert.Null(
            failedMessage.ClaimToken);

        Assert.Null(
            failedMessage.ClaimedUntilUtc);
    }

    [Fact]
    public async Task ProcessAsync_ShouldMarkAsFailed_WhenMaxRetryCountReached()
    {
        // Arrange
        await using var connection = CreateConnection();

        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var message = new OutboxMessage(
            Guid.NewGuid(),
            "OrderCanceledEvent",
            """{"orderId":123}""",
            DateTime.UtcNow);

        context.OutboxMessages.Add(message);

        await context.SaveChangesAsync();

        // Имитируем 4 предыдущие неудачные попытки.
        for (var i = 0; i < 4; i++)
        {
            message.ScheduleRetry(
                "Предыдущая ошибка.",
                DateTime.UtcNow.AddSeconds(-1));
        }

        await context.SaveChangesAsync();

        var publisher = new FailingMessagePublisher(
            "Ошибка публикации сообщения.");

        var processor = new OutboxProcessor(
            context,
            publisher);

        // Act
        await processor.ProcessAsync(
            CancellationToken.None);

        // Assert
        var failedMessage = await context.OutboxMessages
            .SingleAsync();

        Assert.Equal(
            5,
            failedMessage.RetryCount);

        Assert.NotNull(
            failedMessage.FailedOnUtc);

        Assert.Equal(
            "Ошибка публикации сообщения.",
            failedMessage.Error);

        Assert.Null(
            failedMessage.ProcessedOnUtc);

        Assert.Null(
            failedMessage.NextAttemptOnUtc);

        Assert.Null(
            failedMessage.ClaimToken);

        Assert.Null(
            failedMessage.ClaimedUntilUtc);
    }
    [Fact]
    public async Task ProcessAsync_ShouldNotProcessFailedMessage()
    {
        // Arrange
        await using var connection = CreateConnection();

        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var message = new OutboxMessage(
            Guid.NewGuid(),
            "OrderCanceledEvent",
            """{"orderId":123}""",
            DateTime.UtcNow);

        context.OutboxMessages.Add(message);

        await context.SaveChangesAsync();

        message.MarkAsFailed(
            "Фатальная ошибка.");

        await context.SaveChangesAsync();

        var publisher = new RecordingMessagePublisher();

        var processor = new OutboxProcessor(
            context,
            publisher);

        // Act
        await processor.ProcessAsync(
            CancellationToken.None);

        // Assert
        Assert.Empty(publisher.Messages);

        var failedMessage = await context.OutboxMessages
            .SingleAsync();

        Assert.NotNull(
            failedMessage.FailedOnUtc);

        Assert.Null(
            failedMessage.ProcessedOnUtc);
    }

    [Fact]
    public async Task ProcessAsync_ShouldNotProcessMessage_WhenClaimIsStillActive()
    {
        // Arrange
        await using var connection = CreateConnection();

        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var message = new OutboxMessage(
            Guid.NewGuid(),
            "OrderCanceledEvent",
            """{"orderId":123}""",
            DateTime.UtcNow);

        context.OutboxMessages.Add(message);

        await context.SaveChangesAsync();

        // Имитируем сообщение, уже захваченное другим processor.
        message.Claim(
            Guid.NewGuid(),
            DateTime.UtcNow.AddMinutes(1));

        await context.SaveChangesAsync();

        var publisher = new RecordingMessagePublisher();

        var processor = new OutboxProcessor(
            context,
            publisher);

        // Act
        await processor.ProcessAsync(
            CancellationToken.None);

        // Assert
        Assert.Empty(
            publisher.Messages);

        var storedMessage = await context.OutboxMessages
            .SingleAsync();

        Assert.Null(
            storedMessage.ProcessedOnUtc);

        Assert.NotNull(
            storedMessage.ClaimToken);

        Assert.NotNull(
            storedMessage.ClaimedUntilUtc);
    }

    [Fact]
    public async Task ProcessAsync_ShouldProcessMessage_WhenClaimHasExpired()
    {
        // Arrange
        await using var connection = CreateConnection();

        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var message = new OutboxMessage(
            Guid.NewGuid(),
            "OrderCanceledEvent",
            """{"orderId":123}""",
            DateTime.UtcNow);

        context.OutboxMessages.Add(message);

        await context.SaveChangesAsync();

        // Claim уже истёк.
        message.Claim(
            Guid.NewGuid(),
            DateTime.UtcNow.AddMinutes(-1));

        await context.SaveChangesAsync();

        var publisher = new RecordingMessagePublisher();

        var processor = new OutboxProcessor(
            context,
            publisher);

        // Act
        await processor.ProcessAsync(
            CancellationToken.None);

        // Assert
        Assert.Single(
            publisher.Messages);

        var storedMessage = await context.OutboxMessages
            .SingleAsync();

        Assert.NotNull(
            storedMessage.ProcessedOnUtc);

        Assert.Null(
            storedMessage.ClaimToken);

        Assert.Null(
            storedMessage.ClaimedUntilUtc);
    }
    [Fact]
    public async Task ProcessAsync_ShouldClaimMessageOnlyOnce_WhenTwoProcessorsRunConcurrently()
    {
        // Arrange
        await using var connection = CreateConnection();

        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var message = new OutboxMessage(
            Guid.NewGuid(),
            "OrderCanceledEvent",
            """{"orderId":123}""",
            DateTime.UtcNow);

        context.OutboxMessages.Add(message);

        await context.SaveChangesAsync();

        var publisher = new BlockingMessagePublisher();

        await using var context1 = CreateContext(connection);
        await using var context2 = CreateContext(connection);

        var processor1 = new OutboxProcessor(
            context1,
            publisher);

        var processor2 = new OutboxProcessor(
            context2,
            publisher);

        // Act
        var task1 = processor1.ProcessAsync(
            CancellationToken.None);

        await publisher.WaitUntilPublishStartedAsync();

        var task2 = processor2.ProcessAsync(
            CancellationToken.None);

        // Разрешаем первый publisher продолжить.
        publisher.Release();

        await Task.WhenAll(task1, task2);

        // Assert
        Assert.Single(publisher.Messages);
    }

    private static SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(
            "Data Source=:memory:");

        connection.Open();

        return connection;
    }

    private static AppDbContext CreateContext(
        SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var serializer = new OutboxMessageSerializer();

        return new AppDbContext(
            options,
            serializer);
    }

    private sealed class TestMessagePublisher
        : IMessagePublisher
    {
        public BrokerMessage? PublishedMessage { get; private set; }

        public Task PublishAsync(
            BrokerMessage message,
            CancellationToken cancellationToken)
        {
            PublishedMessage = message;

            return Task.CompletedTask;
        }
    }
    private sealed class FailingMessagePublisher(string error) : IMessagePublisher
    {
        public Task PublishAsync(
            BrokerMessage message,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(error);
        }
    }
    private sealed class RecordingMessagePublisher : IMessagePublisher
    {
        public List<BrokerMessage> Messages { get; } = [];

        public Task PublishAsync(
            BrokerMessage message,
            CancellationToken cancellationToken)
        {
            Messages.Add(message);

            return Task.CompletedTask;
        }
    }
}

public sealed class BlockingMessagePublisher : IMessagePublisher
{
    private readonly TaskCompletionSource<bool> _publishStarted =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource<bool> _release =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public readonly List<BrokerMessage> _messages = [];

    public IReadOnlyCollection<BrokerMessage> Messages =>
        _messages;

    public async Task PublishAsync(
        BrokerMessage message,
        CancellationToken cancellationToken)
    {
        _messages.Add(message);

        _publishStarted.TrySetResult(true);

        await _release.Task.WaitAsync(cancellationToken);
    }

    public Task WaitUntilPublishStartedAsync() =>
        _publishStarted.Task;

    public void Release() =>
        _release.TrySetResult(true);
}