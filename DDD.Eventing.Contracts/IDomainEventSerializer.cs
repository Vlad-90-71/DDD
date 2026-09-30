using DDD.Domain.Common.Events;

namespace DDD.Eventing.Contracts;

public interface IDomainEventSerializer
{
    string Serialize(IDomainEvent domainEvent);
    IDomainEvent Deserialize(Guid eventId, string typeName, string content);
}
