using DDD.Infrastructure.Outbox;

namespace DDD.Tests.Infrastructure.Outbox;

public sealed class OutboxMessageTests
{
    private static OutboxMessage CreateMessage()
    {
        return new OutboxMessage(
            Guid.NewGuid(),
            "OrderCanceledEvent",
            """{"orderId":123}""",
            DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_ShouldInitializeMessage()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var occurredOn = DateTime.UtcNow;

        // Act
        var message = new OutboxMessage(
            eventId,
            "OrderCanceledEvent",
            """{"orderId":123}""",
            occurredOn);

        // Assert
        Assert.Equal(eventId, message.EventId);
        Assert.Equal("OrderCanceledEvent", message.Type);
        Assert.Equal("""{"orderId":123}""", message.Content);
        Assert.Equal(occurredOn, message.OccurredOnUtc);

        Assert.Null(message.ProcessedOnUtc);
        Assert.Null(message.FailedOnUtc);
        Assert.Null(message.Error);
        Assert.Null(message.ClaimToken);
        Assert.Null(message.ClaimedUntilUtc);
        Assert.Null(message.NextAttemptOnUtc);

        Assert.Equal(0, message.RetryCount);
    }

    [Fact]
    public void Claim_ShouldSetClaimTokenAndExpiration()
    {
        // Arrange
        var message = CreateMessage();

        var token = Guid.NewGuid();
        var claimedUntil = DateTime.UtcNow.AddMinutes(1);

        // Act
        message.Claim(token, claimedUntil);

        // Assert
        Assert.Equal(token, message.ClaimToken);
        Assert.Equal(claimedUntil, message.ClaimedUntilUtc);
    }

    [Fact]
    public void MarkAsProcessed_ShouldSetProcessedState()
    {
        // Arrange
        var message = CreateMessage();

        var claimToken = Guid.NewGuid();

        message.Claim(
            claimToken,
            DateTime.UtcNow.AddMinutes(1));

        message.ScheduleRetry(
            "Предыдущая ошибка.",
            DateTime.UtcNow.AddSeconds(10));

        var processedOn = DateTime.UtcNow;

        // Act
        message.MarkAsProcessed(processedOn);

        // Assert
        Assert.Equal(processedOn, message.ProcessedOnUtc);

        Assert.Null(message.ClaimToken);
        Assert.Null(message.ClaimedUntilUtc);

        Assert.Null(message.Error);
        Assert.Null(message.NextAttemptOnUtc);

        Assert.Null(message.FailedOnUtc);

        // RetryCount намеренно не сбрасывается.
        Assert.Equal(1, message.RetryCount);
    }

    [Fact]
    public void ScheduleRetry_ShouldIncreaseRetryCountAndSetNextAttempt()
    {
        // Arrange
        var message = CreateMessage();

        var nextAttempt = DateTime.UtcNow.AddSeconds(2);

        // Act
        message.ScheduleRetry(
            "Ошибка публикации.",
            nextAttempt);

        // Assert
        Assert.Equal(1, message.RetryCount);

        Assert.Equal(
            "Ошибка публикации.",
            message.Error);

        Assert.Equal(
            nextAttempt,
            message.NextAttemptOnUtc);

        Assert.Null(message.ClaimToken);
        Assert.Null(message.ClaimedUntilUtc);

        Assert.Null(message.ProcessedOnUtc);
        Assert.Null(message.FailedOnUtc);
    }

    [Fact]
    public void ScheduleRetry_ShouldIncreaseRetryCountOnEveryRetry()
    {
        // Arrange
        var message = CreateMessage();

        // Act
        message.ScheduleRetry(
            "Ошибка 1.",
            DateTime.UtcNow.AddSeconds(1));

        message.ScheduleRetry(
            "Ошибка 2.",
            DateTime.UtcNow.AddSeconds(2));

        message.ScheduleRetry(
            "Ошибка 3.",
            DateTime.UtcNow.AddSeconds(3));

        // Assert
        Assert.Equal(3, message.RetryCount);
        Assert.Equal("Ошибка 3.", message.Error);
    }

    [Fact]
    public void MarkAsFailed_ShouldSetFailedState()
    {
        // Arrange
        var message = CreateMessage();

        var token = Guid.NewGuid();

        message.Claim(
            token,
            DateTime.UtcNow.AddMinutes(1));

        // Act
        message.MarkAsFailed(
            "Фатальная ошибка.");

        // Assert
        Assert.Equal(1, message.RetryCount);

        Assert.Equal(
            "Фатальная ошибка.",
            message.Error);

        Assert.NotNull(message.FailedOnUtc);

        Assert.Null(message.ProcessedOnUtc);
        Assert.Null(message.NextAttemptOnUtc);

        Assert.Null(message.ClaimToken);
        Assert.Null(message.ClaimedUntilUtc);
    }

    [Fact]
    public void Retry_ShouldResetFailedState()
    {
        // Arrange
        var message = CreateMessage();

        message.MarkAsFailed(
            "Фатальная ошибка.");

        Assert.NotNull(message.FailedOnUtc);

        // Act
        message.Retry();

        // Assert
        Assert.Null(message.FailedOnUtc);
        Assert.Null(message.NextAttemptOnUtc);
        Assert.Null(message.Error);

        Assert.Null(message.ClaimToken);
        Assert.Null(message.ClaimedUntilUtc);

        Assert.Null(message.ProcessedOnUtc);

        // RetryCount не сбрасывается.
        Assert.Equal(1, message.RetryCount);
    }

    [Fact]
    public void Retry_ShouldThrow_WhenMessageIsNotFailed()
    {
        // Arrange
        var message = CreateMessage();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => message.Retry());

        Assert.Equal(
            "Повторно обработать можно только завершившееся ошибкой Outbox-сообщение.",
            exception.Message);
    }
}