namespace DDD.Domain.Common.Events;

public abstract record DomainEvent 
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    public abstract string Message { get; }
}