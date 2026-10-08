using DDD.Domain.Common.Events;

namespace DDD.Eventing.Contracts;

public enum EventProcessingClaimStatus
{
    Claimed, AlreadyProcessed, InProgress, Retry, Failed, NoHandler
}
public sealed record EventProcessingClaimResult(EventProcessingClaimStatus Status, Guid EventId, Guid? ClaimToken);

public interface IEventProcessingStore
{
    Task<EventProcessingClaimResult> TryClaimAsync(Guid eventId, CancellationToken cancellationToken);
    Task MarkAsProcessedAsync(Guid eventId, Guid claimToken, CancellationToken cancellationToken);
    Task<bool> ReleaseClaimAsync(Guid eventId, Guid claimToken, string error, CancellationToken cancellationToken);
}