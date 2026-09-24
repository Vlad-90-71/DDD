namespace DDD.MessageBroker.Contracts;

public sealed record BrokerDelivery(
    Guid DeliveryId,
    BrokerMessage Message);