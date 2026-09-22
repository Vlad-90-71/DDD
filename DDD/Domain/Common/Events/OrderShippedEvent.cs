using DDD.Domain.Common.Events;

namespace DDD.Domain.Entities.Events;

public sealed record OrderShippedEvent(int OrderId) : IDomainEvent;