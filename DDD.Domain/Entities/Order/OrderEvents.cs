using DDD.Domain.Common.Events;

namespace DDD.Domain.Entities;

public sealed record OrderProcessingStartedEvent(int OrderId) : DomainEvent
{
    public override string Message =>
        $"Заказ {OrderId} переведен в обработку.";
}

public sealed record OrderShippedEvent(int OrderId) : DomainEvent
{
    public override string Message =>
        $"Заказ {OrderId} был доставлен.";
}

public sealed record OrderCanceledEvent(int OrderId) : DomainEvent
{
    public override string Message =>
        $"Заказ {OrderId} отменен.";
}
