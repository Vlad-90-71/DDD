namespace DDD.Eventing.Contracts;

public interface IDomainEventMetrics
{
    void EventProcessed();
    void EventFailed();
    void OutboxPublished();
    void SetEventClaimCounts(long inProgress, long expired);
}