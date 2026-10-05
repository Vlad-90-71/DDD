using DDD.Application.Common.Events;
using DDD.Domain.Common.Events;
using DDD.Domain.Entities.Order;
using DDD.Eventing.Contracts;
using DDD.Infrastructure;
using DDD.Infrastructure.Events;
using DDD.Infrastructure.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

namespace DDD.Tests.Application;

public sealed class DomainEventHandlerTests
{/*
    [Fact]
    public async Task HandleAsync_ShouldLogAndMarkAsProcessed_WhenEventIsNotProcessed()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var domainEvent = new TestDomainEvent(
            eventId,
            123);

        var eventStore = new TestEventProcessingStore
        {
            IsProcessed = false
        };

        var handler =
            new DomainEventHandler<TestDomainEvent>(
                eventStore);

        // Act
        await handler.HandleAsync(
            domainEvent,
            CancellationToken.None);

        // Assert
        Assert.True(
            eventStore.IsProcessedChecked);

        Assert.Equal(
            eventId,
            eventStore.LoggedEventId);

        Assert.Equal(
            nameof(TestDomainEvent),
            eventStore.LoggedEventType);

        Assert.Equal(
            "Order 123",
            eventStore.LoggedMessage);

        Assert.Equal(
            eventId,
            eventStore.MarkedAsProcessedEventId);
    }

    [Fact]
    public async Task HandleAsync_ShouldDoNothing_WhenEventIsAlreadyProcessed()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var domainEvent = new TestDomainEvent(
            eventId,
            123);

        var eventStore = new TestEventProcessingStore
        {
            IsProcessed = true
        };

        var handler =
            new DomainEventHandler<TestDomainEvent>(
                eventStore);

        // Act
        await handler.HandleAsync(
            domainEvent,
            CancellationToken.None);

        // Assert
        Assert.True(
            eventStore.IsProcessedChecked);

        Assert.Null(
            eventStore.LoggedEventId);

        Assert.Null(
            eventStore.LoggedEventType);

        Assert.Null(
            eventStore.LoggedMessage);

        Assert.Null(
            eventStore.MarkedAsProcessedEventId);
    }

    [Fact]
    public async Task HandleAsync_ShouldUseEventId_WhenCheckingProcessedState()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var domainEvent = new TestDomainEvent(
            eventId,
            123);

        var eventStore = new TestEventProcessingStore();

        var handler =
            new DomainEventHandler<TestDomainEvent>(
                eventStore);

        // Act
        await handler.HandleAsync(
            domainEvent,
            CancellationToken.None);

        // Assert
        Assert.Equal(
            eventId,
            eventStore.CheckedEventId);
    }


    [Fact]
    public async Task HandleAsync_ShouldBeIdempotent_WhenSameEventIsHandledTwice()
    {
        // Arrange
        await using var connection = CreateConnection();
        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var domainEvent = new OrderCanceledEvent(123);

        var eventStore = new EventProcessingStore(context);

        var handler =
            new DomainEventHandler<OrderCanceledEvent>(eventStore);

        // Act
        await handler.HandleAsync(
            domainEvent,
            CancellationToken.None);

        await context.SaveChangesAsync();

        await handler.HandleAsync(
            domainEvent,
            CancellationToken.None);

        await context.SaveChangesAsync();

        // Assert
        var processedEvents =
            await context.ProcessedEvents
                .Where(x => x.EventId == domainEvent.EventId)
                .ToListAsync();

        var eventLogs =
            await context.EventLogs
                .Where(x => x.EventId == domainEvent.EventId)
                .ToListAsync();

        Assert.Single(processedEvents);
        Assert.Single(eventLogs);
    }

    private static SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(
            "Data Source=:memory:");

        connection.Open();

        return connection;
    }

    private static AppDbContext CreateContext(
        SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var serializer = new OutboxMessageSerializer();

        return new AppDbContext(
            options,
            serializer);
    }

    private sealed record TestDomainEvent(
        Guid EventId,
        int OrderId) : IDomainEvent
    {
        public DateTime OccurredOnUtc { get; } =
            DateTime.UtcNow;

        public string Message =>
            $"Order {OrderId}";
    }
    
    private sealed class TestEventProcessingStore
        : IEventProcessingStore
    {
        public bool IsProcessed { get; init; }

        public bool IsProcessedChecked { get; private set; }

        public Guid? CheckedEventId { get; private set; }

        public Guid? LoggedEventId { get; private set; }

        public string? LoggedEventType { get; private set; }

        public string? LoggedMessage { get; private set; }

        public Guid? MarkedAsProcessedEventId { get; private set; }

        public Task<bool> IsProcessedAsync(
            Guid eventId,
            CancellationToken cancellationToken)
        {
            IsProcessedChecked = true;
            CheckedEventId = eventId;

            return Task.FromResult(IsProcessed);
        }

        public void LogProcessed(IDomainEvent domainEvent)
        {
            LoggedEventId = domainEvent.EventId;
            LoggedEventType = domainEvent.GetType().Name;
            LoggedMessage = domainEvent.Message;
        }

        public void MarkAsProcessed(
            Guid eventId)
        {
            MarkedAsProcessedEventId = eventId;
        }
    }*/
}