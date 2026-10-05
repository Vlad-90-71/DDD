using DDD.Application.Common.Events;
using DDD.Domain.Common.ValueObjects;
using DDD.Domain.Entities.Order;
using DDD.Eventing.Contracts;
using DDD.Infrastructure;
using DDD.Infrastructure.Events;
using DDD.Infrastructure.Outbox;
using DDD.MessageBroker.InMemory;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DDD.Tests.Integration;

public sealed class EventFlowTests
{
    [Fact]
    public async Task OrderCanceled_ShouldFlowThroughOutboxAndConsumer_AndBeProcessed()
    {
        // Arrange
        await using var connection = CreateConnection();

        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var broker = new InMemoryMessageBroker();

        var publisher =
            new InMemoryMessagePublisher(broker);

        var order = new Order(
            "Адрес ул 55",
            new CustomerName("Иван Иванов"),
            Email.Create("ivan@example.com"),
            new Money(100m, "EUR"));

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        Assert.NotEqual(0, order.Id);

        order.Cancel();

        var domainEvent =
            order.DomainEvents.Single();

        var eventId = domainEvent.EventId;

        // --------------------------------------------------
        // 1. Domain Event -> Outbox
        // --------------------------------------------------

        await context.SaveChangesAsync();

        var outboxMessage =
            await context.OutboxMessages
                .SingleAsync();

        Assert.Equal(
            eventId,
            outboxMessage.EventId);

        Assert.Equal(
            typeof(OrderCanceledEvent)
                .AssemblyQualifiedName,
            outboxMessage.Type);

        Assert.Null(
            outboxMessage.ProcessedOnUtc);

        // --------------------------------------------------
        // 2. Outbox -> Broker
        // --------------------------------------------------

        var processor =
            new OutboxProcessor(
                context,
                publisher);

        await processor.ProcessAsync(
            CancellationToken.None);

        outboxMessage =
            await context.OutboxMessages
                .SingleAsync();

        Assert.NotNull(
            outboxMessage.ProcessedOnUtc);

        // --------------------------------------------------
        // 3. Broker -> Domain Event
        // --------------------------------------------------

        var delivery =
            await broker.ReceiveAsync(
                CancellationToken.None);

        Assert.Equal(
            eventId,
            delivery.Message.EventId);

        var serializer =
            new DomainEventSerializer();

        var restoredEvent =
            serializer.Deserialize(
                delivery.Message.EventId,
                delivery.Message.Type,
                delivery.Message.Content);

        Assert.Equal(
            eventId,
            restoredEvent.EventId);

        // --------------------------------------------------
        // 4. Domain Event -> Dispatcher -> Handler
        // --------------------------------------------------

        var services =
            new ServiceCollection();

        services.AddScoped<AppDbContext>(_ =>
            CreateContext(connection));

        services.AddScoped<
            IEventProcessingStore,
            EventProcessingStore>();

        services.AddScoped<
            IDomainEventHandler<OrderCanceledEvent>,
            DomainEventHandler<OrderCanceledEvent>>();

        services.AddScoped<IDomainEventHandler>(
            sp =>
                sp.GetRequiredService<
                    IDomainEventHandler<OrderCanceledEvent>>());

        services.AddScoped<
            IDomainEventDispatcher,
            DomainEventDispatcher>();

        await using var provider =
            services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var dispatcher =
                scope.ServiceProvider
                    .GetRequiredService<
                        IDomainEventDispatcher>();

            await dispatcher.DispatchAsync(
                restoredEvent,
                CancellationToken.None);

            var eventContext =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            await eventContext.SaveChangesAsync();
        }

        // --------------------------------------------------
        // 5. ACK
        // --------------------------------------------------

        broker.Ack(
            delivery.DeliveryId);

        // --------------------------------------------------
        // 6. Verify persisted state
        // --------------------------------------------------

        await using var verificationContext =
            CreateContext(connection);

        var processedEvent =
            await verificationContext.ProcessedEvents
                .SingleAsync();

        Assert.Equal(
            eventId,
            processedEvent.EventId);

        var eventLog =
            await verificationContext.EventLogs
                .SingleAsync();

        Assert.Equal(
            eventId,
            eventLog.EventId);

        Assert.Equal(
            nameof(OrderCanceledEvent),
            eventLog.EventType);

        Assert.Equal(
            $"Заказ {order.Id} отменен.",
            eventLog.Message);
    }

    [Fact]
    public async Task OrderCanceled_ShouldBeReprocessed_WhenHandlerFailsFirstAttempt()
    {
        // Arrange
        await using var connection = CreateConnection();

        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var broker =
            new InMemoryMessageBroker();

        var publisher =
            new InMemoryMessagePublisher(broker);

        var order =
            new Order(
                "Адрес ул 55",
                new CustomerName("Иван Иванов"),
                Email.Create("ivan@example.com"),
                new Money(100m, "EUR"));

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        Assert.NotEqual(0, order.Id);

        order.Cancel();

        var domainEvent =
            order.DomainEvents.Single();

        var eventId =
            domainEvent.EventId;

        // --------------------------------------------------
        // 1. Domain Event -> Outbox
        // --------------------------------------------------

        await context.SaveChangesAsync();

        var outboxMessage =
            await context.OutboxMessages
                .SingleAsync();

        Assert.Equal(
            eventId,
            outboxMessage.EventId);

        // --------------------------------------------------
        // 2. DI
        // --------------------------------------------------

        var services =
            new ServiceCollection();

        services.AddScoped<AppDbContext>(_ =>
            CreateContext(connection));

        services.AddScoped<
            IEventProcessingStore,
            EventProcessingStore>();

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddSingleton<
            FailOnceOrderCanceledHandlerState>();

        services.AddScoped<
            FailOnceOrderCanceledHandler>();

        services.AddScoped<
            IDomainEventHandler<OrderCanceledEvent>>(
            sp =>
                sp.GetRequiredService<
                    FailOnceOrderCanceledHandler>());

        services.AddScoped<IDomainEventHandler>(
            sp =>
                sp.GetRequiredService<
                    IDomainEventHandler<OrderCanceledEvent>>());

        services.AddScoped<
            IDomainEventDispatcher,
            DomainEventDispatcher>();

        await using var provider =
            services.BuildServiceProvider();

        // --------------------------------------------------
        // 3. Consumer
        // --------------------------------------------------

        var serializer =
            new DomainEventSerializer();

        var consumer =
            new InMemoryMessageConsumer(
                broker,
                serializer,
                provider.GetRequiredService<
                    IServiceScopeFactory>());

        using var consumerCts =
            new CancellationTokenSource();

        await consumer.StartAsync(
            consumerCts.Token);

        var handler =
            provider.GetRequiredService<
                FailOnceOrderCanceledHandler>();

        // --------------------------------------------------
        // 4. Outbox -> Broker
        // --------------------------------------------------

        var processor =
            new OutboxProcessor(
                context,
                publisher);

        await processor.ProcessAsync(
            CancellationToken.None);

        // Outbox должен быть отмечен как processed
        // сразу после публикации в broker.
        var processedOutboxMessage =
            await context.OutboxMessages
                .SingleAsync();

        Assert.NotNull(
            processedOutboxMessage.ProcessedOnUtc);

        // --------------------------------------------------
        // 5. First attempt must fail
        // --------------------------------------------------

        await handler.WaitUntilFirstAttemptAsync();

        // --------------------------------------------------
        // 6. Second attempt must succeed
        // --------------------------------------------------

        await handler.WaitUntilSecondAttemptAsync();

        // --------------------------------------------------
        // 7. Verify persisted event processing
        // --------------------------------------------------

        await WaitUntilAsync(
            async () =>
            {
                await using var verificationContext =
                    CreateContext(connection);

                return await verificationContext.ProcessedEvents
                    .AnyAsync(x => x.EventId == eventId);
            });

        await consumer.StopAsync(
            CancellationToken.None);

        // --------------------------------------------------
        // 8. Verify database state
        // --------------------------------------------------

        await using var finalContext =
            CreateContext(connection);

        var processedEvent =
            await finalContext.ProcessedEvents
                .SingleAsync(x => x.EventId == eventId);

        Assert.Equal(
            eventId,
            processedEvent.EventId);

        var eventLog =
            await finalContext.EventLogs
                .SingleAsync(x => x.EventId == eventId);

        Assert.Equal(
            eventId,
            eventLog.EventId);

        Assert.Equal(
            nameof(OrderCanceledEvent),
            eventLog.EventType);

        Assert.Equal(
            $"Заказ {order.Id} отменен.",
            eventLog.Message);

        Assert.Equal(
            2,
            handler.AttemptCount);
    }

    private static async Task WaitUntilAsync(
    Func<Task<bool>> condition,
    TimeSpan? timeout = null)
    {
        var limit =
            DateTime.UtcNow.Add(
                timeout ?? TimeSpan.FromSeconds(5));

        while (DateTime.UtcNow < limit)
        {
            if (await condition())
                return;

            await Task.Delay(
                TimeSpan.FromMilliseconds(50));
        }

        throw new TimeoutException(
            "Ожидаемое состояние не наступило за отведённое время.");
    }

    private sealed class FailOnceOrderCanceledHandler(
        IEventProcessingStore eventStore,
        FailOnceOrderCanceledHandlerState state)
        : IDomainEventHandler<OrderCanceledEvent>
    {
        public int AttemptCount =>
            state.AttemptCount;

        public Task WaitUntilFirstAttemptAsync() =>
            state.WaitUntilFirstAttemptAsync();

        public Task WaitUntilSecondAttemptAsync() =>
            state.WaitUntilSecondAttemptAsync();

        public async Task HandleAsync(
            OrderCanceledEvent domainEvent,
            CancellationToken cancellationToken)
        {
            var attempt =
                state.IncrementAttempt();

            if (attempt == 1)
            {
                state.SignalFirstAttempt();

                throw new InvalidOperationException(
                    "Ошибка первой обработки.");
            }

            state.SignalSecondAttempt();

            var realHandler =
                new DomainEventHandler<OrderCanceledEvent>(
                    eventStore);

            await realHandler.HandleAsync(
                domainEvent,
                cancellationToken);
        }
    }
    private sealed class FailOnceOrderCanceledHandlerState
    {
        private readonly TaskCompletionSource<bool> _firstAttempt =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource<bool> _secondAttempt =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _attemptCount;

        public int AttemptCount =>
            Volatile.Read(ref _attemptCount);

        public int IncrementAttempt() =>
            Interlocked.Increment(ref _attemptCount);

        public void SignalFirstAttempt() =>
            _firstAttempt.TrySetResult(true);

        public void SignalSecondAttempt() =>
            _secondAttempt.TrySetResult(true);

        public Task WaitUntilFirstAttemptAsync() =>
            _firstAttempt.Task.WaitAsync(
                TimeSpan.FromSeconds(5));

        public Task WaitUntilSecondAttemptAsync() =>
            _secondAttempt.Task.WaitAsync(
                TimeSpan.FromSeconds(5));
    }

    private static SqliteConnection CreateConnection()
    {
        var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        connection.Open();

        return connection;
    }

    private static AppDbContext CreateContext(
        SqliteConnection connection)
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

        var serializer =
            new OutboxMessageSerializer();

        return new AppDbContext(
            options,
            serializer);
    }
}