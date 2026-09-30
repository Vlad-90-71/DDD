using DDD.MessageBroker.Contracts;
using DDD.MessageBroker.InMemory;

namespace DDD.Tests.MessageBroker;

public sealed class InMemoryMessageBrokerTests
{
    private static BrokerMessage CreateMessage(
        Guid? eventId = null)
    {
        return new BrokerMessage(
            eventId ?? Guid.NewGuid(),
            "OrderCanceledEvent",
            """{"orderId":123}""");
    }

    [Fact]
    public async Task PublishAsync_ShouldMakeMessageAvailableForReceive()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();

        var message = CreateMessage();

        // Act
        await broker.PublishAsync(
            message,
            CancellationToken.None);

        var delivery = await broker.ReceiveAsync(
            CancellationToken.None);

        // Assert
        Assert.Equal(
            message.EventId,
            delivery.Message.EventId);

        Assert.Equal(
            message.Type,
            delivery.Message.Type);

        Assert.Equal(
            message.Content,
            delivery.Message.Content);

        Assert.NotEqual(
            Guid.Empty,
            delivery.DeliveryId);
    }

    [Fact]
    public async Task ReceiveAsync_ShouldReturnDifferentDeliveryIds_ForDifferentMessages()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();

        var message1 = CreateMessage();
        var message2 = CreateMessage();

        // Act
        await broker.PublishAsync(
            message1,
            CancellationToken.None);

        await broker.PublishAsync(
            message2,
            CancellationToken.None);

        var delivery1 = await broker.ReceiveAsync(
            CancellationToken.None);

        var delivery2 = await broker.ReceiveAsync(
            CancellationToken.None);

        // Assert
        Assert.NotEqual(
            delivery1.DeliveryId,
            delivery2.DeliveryId);

        Assert.Equal(
            message1.EventId,
            delivery1.Message.EventId);

        Assert.Equal(
            message2.EventId,
            delivery2.Message.EventId);
    }

    [Fact]
    public async Task AckAsync_ShouldRemovePendingDelivery()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();

        var message = CreateMessage();

        await broker.PublishAsync(
            message,
            CancellationToken.None);

        var delivery = await broker.ReceiveAsync(
            CancellationToken.None);

        // Act
        broker.Ack(delivery.DeliveryId);

        // Assert
        using var cancellationTokenSource =
            new CancellationTokenSource(
                TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () =>
            {
                await broker.ReceiveAsync(
                    cancellationTokenSource.Token);
            });
    }

    [Fact]
    public async Task AckAsync_ShouldBeSafe_WhenDeliveryDoesNotExist()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();

        // Act & Assert
        broker.Ack(Guid.NewGuid());
    }

    [Fact]
    public async Task NackAsync_ShouldRemoveDelivery_WhenRequeueIsFalse()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();

        var message = CreateMessage();

        await broker.PublishAsync(
            message,
            CancellationToken.None);

        var delivery = await broker.ReceiveAsync(
            CancellationToken.None);

        // Act
        await broker.NackAsync(
            delivery.DeliveryId,
            requeue: false,
            CancellationToken.None);

        // Assert
        using var cancellationTokenSource =
            new CancellationTokenSource(
                TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () =>
            {
                await broker.ReceiveAsync(
                    cancellationTokenSource.Token);
            });
    }

    [Fact]
    public async Task NackAsync_ShouldRequeueDelivery_WhenRequeueIsTrue()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();

        var message = CreateMessage();

        await broker.PublishAsync(
            message,
            CancellationToken.None);

        var firstDelivery = await broker.ReceiveAsync(
            CancellationToken.None);

        // Act
        await broker.NackAsync(
            firstDelivery.DeliveryId,
            requeue: true,
            CancellationToken.None);

        var secondDelivery = await broker.ReceiveAsync(
            CancellationToken.None);

        // Assert
        Assert.Equal(
            message.EventId,
            secondDelivery.Message.EventId);

        Assert.Equal(
            message.Type,
            secondDelivery.Message.Type);

        Assert.Equal(
            message.Content,
            secondDelivery.Message.Content);

        Assert.Equal(
            firstDelivery.DeliveryId,
            secondDelivery.DeliveryId);
    }

    [Fact]
    public async Task NackAsync_ShouldDoNothing_WhenDeliveryDoesNotExist()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();

        // Act & Assert
        await broker.NackAsync(
            Guid.NewGuid(),
            requeue: true,
            CancellationToken.None);
    }

    [Fact]
    public async Task ReceiveAsync_ShouldThrowOperationCanceledException_WhenCancelled()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () =>
            {
                await broker.ReceiveAsync(
                    cancellationTokenSource.Token);
            });
    }

    [Fact]
    public async Task PublishAsync_ShouldThrowOperationCanceledException_WhenCancelled()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();

        var message = CreateMessage();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () =>
            {
                await broker.PublishAsync(
                    message,
                    cancellationTokenSource.Token);
            });
    }

    [Fact]
    public async Task NackAsync_ShouldThrowOperationCanceledException_WhenRequeueIsCancelled()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();

        var message = CreateMessage();

        await broker.PublishAsync(
            message,
            CancellationToken.None);

        var delivery = await broker.ReceiveAsync(
            CancellationToken.None);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () =>
            {
                await broker.NackAsync(
                    delivery.DeliveryId,
                    requeue: true,
                    cancellationTokenSource.Token);
            });
    }
}