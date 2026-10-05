using DDD.Domain.Common.Events;
using DDD.Domain.Entities;
using DDD.Domain.Entities.Order;
using DDD.Eventing.Contracts;
using DDD.Infrastructure.Events;
using DDD.MessageBroker.Contracts;
using DDD.MessageBroker.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace DDD.Tests.MessageBroker;

public sealed class InMemoryMessageConsumerTests
{/*
    [Fact]
    public async Task Consumer_ShouldNotAck_WhenSaveChangesFails()
    {
        // Arrange
        var broker =
            new InMemoryMessageBroker
            {
                RequeueOnNack = false
            };

        var domainEvent =
            new TestDomainEvent(
                Guid.NewGuid(),
                123);

        var serializer =
            new DomainEventSerializer();

        var message =
            new BrokerMessage(
                domainEvent.EventId,
                typeof(TestDomainEvent).AssemblyQualifiedName!,
                serializer.Serialize(domainEvent));

        await broker.PublishAsync(
            message,
            CancellationToken.None);

        var dispatcher =
            new TestDomainEventDispatcher();

        var unitOfWork =
            new TestUnitOfWork
            {
                ShouldFail = true
            };

        var services =
            new ServiceCollection();

        services.AddSingleton<IDomainEventDispatcher>(
            dispatcher);

        services.AddSingleton<IUnitOfWork>(
            unitOfWork);

        await using var provider =
            services.BuildServiceProvider();

        var consumer =
            new InMemoryMessageConsumer(
                broker,
                serializer,
                provider.GetRequiredService<
                    IServiceScopeFactory>());

        using var cts =
            new CancellationTokenSource();

        // Act
        await consumer.StartAsync(
            cts.Token);

        await dispatcher.WaitUntilDispatchedAsync();

        await unitOfWork.WaitUntilSaveAttemptedAsync();

        await broker.WaitUntilNackedAsync();

        // Assert
        Assert.Equal(
            1,
            unitOfWork.SaveChangesCount);

        Assert.Equal(
            1,
            broker.NackCount);

        Assert.Equal(
            0,
            broker.AckCount);

        // Stop consumer.
        await cts.CancelAsync();

        await consumer.StopAsync(
            CancellationToken.None);
    }

    [Fact]
    public async Task Consumer_ShouldDispatchMessageAndAck_WhenProcessingSucceeds()
    {
        // Arrange
        var broker =
            new InMemoryMessageBroker();

        var domainEvent =
            new TestDomainEvent(
                Guid.NewGuid(),
                123);

        var serializer =
            new DomainEventSerializer();

        var message =
            new BrokerMessage(
                domainEvent.EventId,
                typeof(TestDomainEvent).AssemblyQualifiedName!,
                serializer.Serialize(domainEvent));

        await broker.PublishAsync(
            message,
            CancellationToken.None);

        var dispatcher =
            new TestDomainEventDispatcher();

        var unitOfWork =
            new TestUnitOfWork();

        var services =
            new ServiceCollection();

        services.AddSingleton<IDomainEventDispatcher>(
            dispatcher);

        services.AddSingleton<IUnitOfWork>(
            unitOfWork);

        await using var provider =
            services.BuildServiceProvider();

        var consumer =
            new InMemoryMessageConsumer(
                broker,
                serializer,
                provider.GetRequiredService<
                    IServiceScopeFactory>());

        using var cts =
            new CancellationTokenSource();

        // Act
        await consumer.StartAsync(
            cts.Token);

        await dispatcher.WaitUntilDispatchedAsync();

        await unitOfWork.WaitUntilSavedAsync();

        // Даём ExecuteAsync завершить ACK.
        await Task.Yield();

        // Assert
        Assert.NotNull(
            dispatcher.DispatchedEvent);

        Assert.Equal(
            message.EventId,
            dispatcher.DispatchedEvent!.EventId);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCount);

        // После ACK сообщение не должно быть доступно
        // для повторного получения.
        using var receiveCts =
            new CancellationTokenSource(
                TimeSpan.FromMilliseconds(500));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () =>
            {
                await broker.ReceiveAsync(
                    receiveCts.Token);
            });

        await cts.CancelAsync();

        await consumer.StopAsync(
            CancellationToken.None);
    }

    [Fact]
    public async Task Consumer_ShouldNackAndRequeue_WhenDispatcherThrows()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();

        var serializer = new TestDomainEventSerializer();

        var dispatcher =
            new FailingDomainEventDispatcher();

        var services = new ServiceCollection();

        services.AddScoped<IDomainEventDispatcher>(
            _ => dispatcher);

        await using var provider =
            services.BuildServiceProvider();

        var consumer = new InMemoryMessageConsumer(
            broker,
            serializer,
            provider.GetRequiredService<IServiceScopeFactory>());

        using var cts = new CancellationTokenSource();

        await consumer.StartAsync(cts.Token);

        var message = new BrokerMessage(
            Guid.NewGuid(),
            nameof(TestDomainEvent),
            """{"orderId":123}""");

        await broker.PublishAsync(
            message,
            CancellationToken.None);

        // Act
        await dispatcher.WaitUntilDispatchedAsync();

        // После NACK сообщение должно вернуться в broker.
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

        broker.Ack(delivery.DeliveryId);

        await consumer.StopAsync(
            CancellationToken.None);
    }

    [Fact]
    public async Task Consumer_ShouldNackAndRequeue_WhenDeserializationFails()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();

        var serializer =
            new FailingDomainEventSerializer();

        var dispatcher =
            new RecordingDomainEventDispatcher();

        var services = new ServiceCollection();

        services.AddScoped<IDomainEventDispatcher>(
            _ => dispatcher);

        await using var provider =
            services.BuildServiceProvider();

        var consumer = new InMemoryMessageConsumer(
            broker,
            serializer,
            provider.GetRequiredService<IServiceScopeFactory>());

        using var cts = new CancellationTokenSource();

        await consumer.StartAsync(cts.Token);

        var message = new BrokerMessage(
            Guid.NewGuid(),
            nameof(TestDomainEvent),
            """{"orderId":123}""");

        await broker.PublishAsync(
            message,
            CancellationToken.None);

        // Act
        await serializer.WaitUntilDeserializeStartedAsync();

        // Assert
        using var receiveCts =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(1));

        var delivery = await broker.ReceiveAsync(
            receiveCts.Token);

        Assert.Equal(
            message.EventId,
            delivery.Message.EventId);

        Assert.Empty(
            dispatcher.Events);

        broker.Ack(delivery.DeliveryId);

        await consumer.StopAsync(
            CancellationToken.None);
    }

    [Fact]
    public void Deserialize_ShouldPreserveEventId()
    {
        // Arrange
        var serializer = new DomainEventSerializer();

        var original = new OrderCanceledEvent(123);

        var content = serializer.Serialize(original);

        // Act
        var restored = serializer.Deserialize(
            original.EventId,
            original.GetType().AssemblyQualifiedName!,
            content);

        // Assert
        Assert.Equal(
            original.EventId,
            restored.EventId);

        Assert.Equal(
            original.OccurredOnUtc,
            restored.OccurredOnUtc);

        Assert.Equal(
            original.Message,
            restored.Message);
    }

    [Fact]
    public void Deserialize_ShouldThrow_WhenEventIdDoesNotMatch()
    {
        // Arrange
        var serializer = new DomainEventSerializer();

        var original = new OrderCanceledEvent(123);

        var content = serializer.Serialize(original);

        var differentEventId = Guid.NewGuid();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            serializer.Deserialize(
                differentEventId,
                original.GetType().AssemblyQualifiedName!,
                content));

        Assert.Contains(
            "EventId не совпадает",
            exception.Message);
    }
    */
    // -------------------------------------------------------
    // Test domain event
    // -------------------------------------------------------
    /*
    private sealed record TestDomainEvent(
        int OrderId) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();

        public DateTime OccurredOnUtc { get; } =
            DateTime.UtcNow;

        public string Message =>
            $"Order {OrderId}";
    }
    */
    private sealed record TestDomainEvent(Guid EventId, int OrderId) : IDomainEvent
    {
        public DateTime OccurredOnUtc { get; } =
            DateTime.UtcNow;

        public string Message =>
            $"Order {OrderId}";
    }
    // -------------------------------------------------------
    // Serializer
    // -------------------------------------------------------

    private sealed class TestDomainEventSerializer
        : IDomainEventSerializer
    {
        public IDomainEvent Deserialize(Guid eventId, string typeName, string content)
        {
            return new TestDomainEvent(eventId, 123);
        }

        public string Serialize(IDomainEvent domainEvent)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FailingDomainEventSerializer
        : IDomainEventSerializer
    {
        private readonly TaskCompletionSource<bool>
            _deserializeStarted =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IDomainEvent Deserialize(Guid eventId, string typeName, string content)
        {
            _deserializeStarted.TrySetResult(true);

            throw new InvalidOperationException(
                "Ошибка десериализации.");
        }

        public string Serialize(IDomainEvent domainEvent)
        {
            throw new NotImplementedException();
        }

        public Task WaitUntilDeserializeStartedAsync() =>
            _deserializeStarted.Task;
    }

    // -------------------------------------------------------
    // Dispatcher
    // -------------------------------------------------------
    /*
    private sealed class RecordingDomainEventDispatcher
        : IDomainEventDispatcher
    {
        private readonly TaskCompletionSource<bool>
            _dispatched =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<IDomainEvent> Events { get; } = [];

        public Task DispatchAsync(
            IDomainEvent domainEvent,
            CancellationToken cancellationToken)
        {
            Events.Add(domainEvent);

            _dispatched.TrySetResult(true);

            return Task.CompletedTask;
        }

        public Task WaitUntilDispatchedAsync() =>
            _dispatched.Task;
    }
    
    private sealed class FailingDomainEventDispatcher
        : IDomainEventDispatcher
    {
        private readonly TaskCompletionSource<bool>
            _dispatched =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task DispatchAsync(
            IDomainEvent domainEvent,
            CancellationToken cancellationToken)
        {
            _dispatched.TrySetResult(true);

            throw new InvalidOperationException(
                "Ошибка обработчика события.");
        }

        public Task WaitUntilDispatchedAsync() =>
            _dispatched.Task;


    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        private readonly TaskCompletionSource<bool> _saved =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource<bool> _saveAttempted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _saveChangesCount;

        public bool ShouldFail { get; init; }

        public int SaveChangesCount =>
            Volatile.Read(ref _saveChangesCount);

        public async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(
                ref _saveChangesCount);

            _saveAttempted.TrySetResult(true);

            if (ShouldFail)
                throw new InvalidOperationException(
                    "SaveChanges failed.");

            _saved.TrySetResult(true);

            return 1;
        }

        public Task WaitUntilSavedAsync() =>
            _saved.Task.WaitAsync(
                TimeSpan.FromSeconds(5));

        public Task WaitUntilSaveAttemptedAsync() =>
            _saveAttempted.Task.WaitAsync(
                TimeSpan.FromSeconds(5));
    }

    private sealed class TestDomainEventDispatcher
    : IDomainEventDispatcher
    {
        private readonly TaskCompletionSource<bool> _dispatched =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IDomainEvent? DispatchedEvent { get; private set; }

        public Task DispatchAsync(
            IDomainEvent domainEvent,
            CancellationToken cancellationToken)
        {
            DispatchedEvent = domainEvent;

            _dispatched.TrySetResult(true);

            return Task.CompletedTask;
        }

        public Task WaitUntilDispatchedAsync() =>
            _dispatched.Task.WaitAsync(
                TimeSpan.FromSeconds(5));
    }
    */
}