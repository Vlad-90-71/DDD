using DDD.Domain.Common.Events;
using DDD.Domain.Entities.Order;
using DDD.Infrastructure;
using DDD.Infrastructure.Events;
using DDD.Infrastructure.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DDD.Tests.Infrastructure;

public sealed class EventProcessingStoreTests
{
    [Fact]
    public async Task IsProcessedAsync_ShouldReturnFalse_WhenEventDoesNotExist()
    {
        // Arrange
        await using var connection = CreateConnection();
        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var store = new EventProcessingStore(context);

        var eventId = Guid.NewGuid();

        // Act
        var result = await store.IsProcessedAsync(
            eventId,
            CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task MarkAsProcessed_ShouldCreateProcessedEvent()
    {
        // Arrange
        await using var connection = CreateConnection();
        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var store = new EventProcessingStore(context);

        var eventId = Guid.NewGuid();

        // Act
        store.MarkAsProcessed(eventId);

        await context.SaveChangesAsync();

        // Assert
        var processedEvent = await context.ProcessedEvents
            .SingleAsync();

        Assert.Equal(
            eventId,
            processedEvent.EventId);

        Assert.NotEqual(
            default,
            processedEvent.ProcessedOnUtc);
    }

    [Fact]
    public async Task IsProcessedAsync_ShouldReturnTrue_AfterEventIsMarkedAsProcessed()
    {
        // Arrange
        await using var connection = CreateConnection();
        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var store = new EventProcessingStore(context);

        var eventId = Guid.NewGuid();

        store.MarkAsProcessed(eventId);

        await context.SaveChangesAsync();

        // Act
        var result = await store.IsProcessedAsync(
            eventId,
            CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task Log_ShouldCreateEventLog()
    {
        // Arrange
        await using var connection = CreateConnection();
        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var store = new EventProcessingStore(context);

        var eventId = Guid.NewGuid();

        // Act
        store.LogProcessed(new OrderCanceledEvent(123));

        await context.SaveChangesAsync();

        // Assert
        var log = await context.EventLogs
            .SingleAsync();

        Assert.Equal(
            eventId,
            log.EventId);

        Assert.Equal(
            "OrderCanceledEvent",
            log.EventType);

        Assert.Equal(
            "Заказ 123 отменен.",
            log.Message);

        Assert.NotEqual(
            default,
            log.CreatedOnUtc);
    }

    [Fact]
    public async Task MarkAsProcessed_ShouldNotAffectEventLog()
    {
        // Arrange
        await using var connection = CreateConnection();
        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var store = new EventProcessingStore(context);

        var eventId = Guid.NewGuid();

        // Act
        store.MarkAsProcessed(eventId);

        await context.SaveChangesAsync();

        // Assert
        Assert.Empty(
            await context.EventLogs.ToListAsync());

        Assert.Single(
            await context.ProcessedEvents.ToListAsync());
    }

    [Fact]
    public async Task Log_ShouldNotCreateProcessedEvent()
    {
        // Arrange
        await using var connection = CreateConnection();
        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var store = new EventProcessingStore(context);

        var eventId = Guid.NewGuid();

        // Act
        store.LogProcessed(new OrderCanceledEvent(123));

        await context.SaveChangesAsync();

        // Assert
        Assert.Empty(
            await context.ProcessedEvents.ToListAsync());

        Assert.Single(
            await context.EventLogs.ToListAsync());
    }

    [Fact]
    public void ProcessedEvent_ShouldUseEventIdAsPrimaryKey()
    {
        // Arrange
        var connection = CreateConnection();

        using var context = CreateContext(connection);

        // Act
        var entityType =
            context.Model.FindEntityType(
                typeof(ProcessedEvent));

        var primaryKey =
            entityType!.FindPrimaryKey();

        // Assert
        Assert.NotNull(primaryKey);

        var property = Assert.Single(
            primaryKey.Properties);

        Assert.Equal(
            nameof(ProcessedEvent.EventId),
            property.Name);
    }

    [Fact]
    public async Task MarkAsProcessed_ShouldMakeEventProcessed()
    {
        // Arrange
        await using var connection = CreateConnection();
        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var eventId = Guid.NewGuid();

        var store = new EventProcessingStore(context);

        // Act
        store.MarkAsProcessed(eventId);

        await context.SaveChangesAsync();

        var isProcessed = await store.IsProcessedAsync(
            eventId,
            CancellationToken.None);

        // Assert
        Assert.True(isProcessed);
    }
    [Fact]
    public async Task IsProcessedAsync_ShouldReturnFalse_ForDifferentEventId()
    {
        // Arrange
        await using var connection = CreateConnection();
        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        var processedEventId = Guid.NewGuid();
        var anotherEventId = Guid.NewGuid();

        var store = new EventProcessingStore(context);

        store.MarkAsProcessed(processedEventId);

        await context.SaveChangesAsync();

        // Act
        var result = await store.IsProcessedAsync(
            anotherEventId,
            CancellationToken.None);

        // Assert
        Assert.False(result);
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