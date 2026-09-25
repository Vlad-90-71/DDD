namespace DDD.Infrastructure.Outbox;

public class OutboxMessage
{
    public long Id { get; private set; }
    public Guid EventId { get; private set; }
    public string Type { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public DateTime OccurredOnUtc { get; private set; }
    public DateTime? ProcessedOnUtc { get; private set; }
    public int RetryCount { get; private set; }
    public string? Error { get; private set; }
    public Guid? ClaimToken { get; private set; }
    public DateTime? ClaimedUntilUtc { get; private set; }
    public DateTime? NextAttemptOnUtc { get; private set; }
    public DateTime? FailedOnUtc { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(Guid eventId, string type, string content, DateTime occurredOnUtc)
    {
        EventId = eventId;
        Type = type;
        Content = content;
        OccurredOnUtc = occurredOnUtc;
    }

    public void Claim(Guid token, DateTime claimedUntilUtc)
    {
        ClaimToken = token;
        ClaimedUntilUtc = claimedUntilUtc;
    }

    public void MarkAsProcessed(DateTime processedOnUtc)
    {
        ProcessedOnUtc = processedOnUtc;

        ClaimToken = null;
        ClaimedUntilUtc = null;

        Error = null;
        NextAttemptOnUtc = null;
    }
    public void ScheduleRetry(string error, DateTime nextAttemptOnUtc)
    {
        RetryCount++;

        Error = error;
        NextAttemptOnUtc = nextAttemptOnUtc;

        ClaimToken = null;
        ClaimedUntilUtc = null;
    }

    public void MarkAsFailed(string error)
    {
        RetryCount++;

        Error = error;
        FailedOnUtc = DateTime.UtcNow;

        NextAttemptOnUtc = null;
        ClaimToken = null;
        ClaimedUntilUtc = null;
    }
    public void Retry()
    {
        if (FailedOnUtc is null)
            throw new InvalidOperationException(
                "Повторно обработать можно только завершившееся ошибкой Outbox-сообщение.");

        FailedOnUtc = null;
        NextAttemptOnUtc = null;
        Error = null;

        ClaimToken = null;
        ClaimedUntilUtc = null;
    }
}