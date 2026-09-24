namespace DDD.MessageBroker.Contracts;

public sealed record BrokerMessage(
    Guid EventId,
    string Type,
    string Content);