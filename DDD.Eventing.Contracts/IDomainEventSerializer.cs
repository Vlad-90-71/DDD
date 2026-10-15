using DDD.Domain.Common.Events;

namespace DDD.Eventing.Contracts;

public interface IDomainEventSerializer
{
    string Serialize(DomainEvent domainEvent);
    DomainEvent Deserialize(Guid eventId, string typeName, string content);
}
