namespace DDD.Application.Common.Messaging;

public sealed record BrokerMessage(
    Guid EventId,
    string Type,
    string Content);