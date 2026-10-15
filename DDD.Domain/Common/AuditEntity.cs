using DDD.Domain.Common.Events;

namespace DDD.Domain.Common;

public interface IAuditable
{
    DateTime CreatedOnUtc { get; init; }
    DateTime UpdatedOnUtc { get; set; } 
}
public abstract class AuditEntity<TKey> : Entity<TKey>, IAuditable where TKey : struct
{
    public DateTime CreatedOnUtc { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedOnUtc { get ; set; } = DateTime.UtcNow;
}
public abstract class AuditEntityEvent<TKey> : EntityEvent<TKey>, IAuditable where TKey : struct
{
    public DateTime CreatedOnUtc { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;
}
