namespace DDD.Infrastructure.Events;

public sealed class EventProcessingState 
{
    public Guid EventId { get; private set; }

    public int AttemptCount { get; private set; }

    public Guid? ClaimToken { get; private set; }

    public DateTime? ClaimedUntilUtc { get; private set; }

    public DateTime? ProcessedOnUtc { get; private set; }

    public DateTime? FailedOnUtc { get; private set; }

    public string? LastError { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public DateTime UpdatedOnUtc { get; private set; }

    private EventProcessingState() { }

    public EventProcessingState(Guid eventId, DateTime now)
    {
        EventId = eventId;
        CreatedOnUtc = now;
        UpdatedOnUtc = now;
    }

    public bool IsProcessed =>
        ProcessedOnUtc is not null;

    public bool IsClaimed(DateTime now) =>
        ProcessedOnUtc is null &&  FailedOnUtc is null &&
        ClaimToken is not null && ClaimedUntilUtc > now;

    public void MarkAsProcessed(Guid claimToken, DateTime processedOnUtc)
    {
        if (ClaimToken != claimToken)
            throw new InvalidOperationException("Событие захвачено другим consumer.");

        ProcessedOnUtc = processedOnUtc;
        ClaimToken = null;
        ClaimedUntilUtc = null;
        FailedOnUtc = null;
        LastError = null;
        UpdatedOnUtc = processedOnUtc;
    }
}