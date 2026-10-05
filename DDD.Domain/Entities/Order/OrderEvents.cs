using DDD.Domain.Common.Events;
using DDD.Domain.Common.ValueObjects;

namespace DDD.Domain.Entities.Order;

public sealed record OrderCreatedEvent(string ProductName, CustomerName CustomerName, Email Email, Money Price) : DomainEvent
{
    public override string Message =>
        $"Создан заказ. Товар: '{ProductName}'. " +
        $"Клиент: '{CustomerName.Value}'. Email: '{Email.Value}'. " +
        $"Цена: {Price.Amount} {Price.Currency}.";
}

public sealed record OrderProcessingRequestedEvent(int OrderId, string ProductName, string Address) : DomainEvent
{
    public override string Message =>
        $"Получен запрос на обработку заказа {OrderId}. " +
        $"Товар: '{ProductName}'. Адрес: '{Address}'.";
}

public sealed record OrderProcessingStartedEvent(int OrderId, string ProductName) : DomainEvent
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
