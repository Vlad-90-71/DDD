using DDD.Domain.Entities;

namespace DDD.Tests.Domain;

public sealed class OrderDomainEventTests
{
    private static Order CreateOrder()
    {
        return new Order(
            new DDD.Domain.Common.ValueObjects.CustomerName("Иван Иванов"),
            DDD.Domain.Common.ValueObjects.Email.Create("ivan@example.com"),
            new DDD.Domain.Common.ValueObjects.Money(100m, "EUR"));
    }

    [Fact]
    public void StartProcessing_ShouldRaiseOrderProcessingStartedEvent()
    {
        // Arrange
        var order = CreateOrder();

        // Act
        order.StartProcessing();

        // Assert
        var domainEvent = Assert.Single(order.DomainEvents);

        var processingEvent =
            Assert.IsType<OrderProcessingStartedEvent>(domainEvent);

        Assert.Equal(order.Id, processingEvent.OrderId);
        Assert.NotEqual(Guid.Empty, processingEvent.EventId);
    }

    [Fact]
    public void StartProcessing_ShouldCreateEventWithMessage()
    {
        // Arrange
        var order = CreateOrder();

        // Act
        order.StartProcessing();

        // Assert
        var domainEvent = Assert.Single(order.DomainEvents);

        Assert.Equal(
            $"Заказ {order.Id} переведен в обработку.",
            domainEvent.Message);
    }

    [Fact]
    public void Ship_ShouldRaiseOrderShippedEvent()
    {
        // Arrange
        var order = CreateOrder();
        order.StartProcessing();

        // Act
        order.Ship();

        // Assert
        Assert.Equal(2, order.DomainEvents.Count);

        var domainEvent =
            Assert.IsType<OrderShippedEvent>(
                order.DomainEvents.Last());

        Assert.Equal(order.Id, domainEvent.OrderId);
        Assert.NotEqual(Guid.Empty, domainEvent.EventId);
    }

    [Fact]
    public void Ship_ShouldCreateEventWithMessage()
    {
        // Arrange
        var order = CreateOrder();
        order.StartProcessing();

        // Act
        order.Ship();

        // Assert
        var domainEvent =
            Assert.IsType<OrderShippedEvent>(
                order.DomainEvents.Last());

        Assert.Equal(
            $"Заказ {order.Id} был доставлен.",
            domainEvent.Message);
    }

    [Fact]
    public void Cancel_ShouldRaiseOrderCanceledEvent()
    {
        // Arrange
        var order = CreateOrder();

        // Act
        order.Cancel();

        // Assert
        var domainEvent = Assert.Single(order.DomainEvents);

        var canceledEvent =
            Assert.IsType<OrderCanceledEvent>(domainEvent);

        Assert.Equal(order.Id, canceledEvent.OrderId);
        Assert.NotEqual(Guid.Empty, canceledEvent.EventId);
    }

    [Fact]
    public void Cancel_ShouldCreateEventWithMessage()
    {
        // Arrange
        var order = CreateOrder();

        // Act
        order.Cancel();

        // Assert
        var domainEvent = Assert.Single(order.DomainEvents);

        Assert.Equal(
            $"Заказ {order.Id} отменен.",
            domainEvent.Message);
    }

    [Fact]
    public void StartProcessing_ShouldSetOccurredOnUtc()
    {
        // Arrange
        var before = DateTime.UtcNow;
        var order = CreateOrder();

        // Act
        order.StartProcessing();
        var after = DateTime.UtcNow;

        // Assert
        var domainEvent = Assert.Single(order.DomainEvents);

        Assert.InRange(
            domainEvent.OccurredOnUtc,
            before,
            after);
    }

    [Fact]
    public void EachDomainEvent_ShouldHaveUniqueEventId()
    {
        // Arrange
        var order = CreateOrder();

        // Act
        order.StartProcessing();
        order.Ship();

        // Assert
        var events = order.DomainEvents.ToList();

        Assert.Equal(2, events.Count);
        Assert.NotEqual(
            events[0].EventId,
            events[1].EventId);
    }

    [Fact]
    public void InvalidTransition_ShouldNotAddNewDomainEvent()
    {
        // Arrange
        var order = CreateOrder();

        order.StartProcessing();

        var eventCountBefore = order.DomainEvents.Count;

        // Act
        Assert.Throws<InvalidOperationException>(() =>
            order.StartProcessing());

        // Assert
        Assert.Equal(
            eventCountBefore,
            order.DomainEvents.Count);
    }

    [Fact]
    public void InvalidShip_ShouldNotAddDomainEvent()
    {
        // Arrange
        var order = CreateOrder();

        // Act
        Assert.Throws<InvalidOperationException>(() =>
            order.Ship());

        // Assert
        Assert.Empty(order.DomainEvents);
    }

    [Fact]
    public void InvalidCancel_ShouldNotAddDomainEvent()
    {
        // Arrange
        var order = CreateOrder();

        order.StartProcessing();
        order.Ship();

        // Act
        Assert.Throws<InvalidOperationException>(() =>
            order.Cancel());

        // Assert
        Assert.Equal(2, order.DomainEvents.Count);
        Assert.IsType<OrderProcessingStartedEvent>(
            order.DomainEvents.First());
        Assert.IsType<OrderShippedEvent>(
            order.DomainEvents.Last());
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var order = CreateOrder();

        order.StartProcessing();
        order.Ship();

        Assert.Equal(2, order.DomainEvents.Count);

        // Act
        order.ClearDomainEvents();

        // Assert
        Assert.Empty(order.DomainEvents);
    }
}