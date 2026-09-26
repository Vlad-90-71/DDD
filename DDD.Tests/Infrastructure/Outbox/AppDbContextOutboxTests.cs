using DDD.Domain.Common.ValueObjects;
using DDD.Domain.Entities;
using DDD.Infrastructure;
using DDD.Infrastructure.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DDD.Tests.Infrastructure.Outbox;

public sealed class AppDbContextOutboxTests
{
    private static SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        return connection;
    }

    private static DbContextOptions<AppDbContext> CreateOptions(
        SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
    }

    private static AppDbContext CreateContext(
        SqliteConnection connection,
        IOutboxMessageSerializer serializer)
    {
        var options = CreateOptions(connection);

        return new AppDbContext(
            options,
            serializer);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldCreateOutboxMessage_WhenOrderRaisesDomainEvent()
    {
        // Arrange
        await using var connection = CreateConnection();

        var serializer = new OutboxMessageSerializer();

        await using var context = CreateContext(
            connection,
            serializer);

        await context.Database.EnsureCreatedAsync();

        var order = new Order(
            new CustomerName("Иван Иванов"),
            Email.Create("ivan@example.com"),
            new Money(100m, "EUR"));

        context.Orders.Add(order);

        order.StartProcessing();

        var domainEvent = Assert.Single(order.DomainEvents);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var outboxMessage = await context.OutboxMessages
            .SingleAsync();

        Assert.Equal(
            domainEvent.EventId,
            outboxMessage.EventId);

        Assert.Equal(
            domainEvent.OccurredOnUtc,
            outboxMessage.OccurredOnUtc);

        Assert.False(string.IsNullOrWhiteSpace(
            outboxMessage.Type));

        Assert.False(string.IsNullOrWhiteSpace(
            outboxMessage.Content));

        Assert.Null(outboxMessage.ProcessedOnUtc);
        Assert.Equal(0, outboxMessage.RetryCount);
        Assert.Null(outboxMessage.Error);
        Assert.Null(outboxMessage.ClaimToken);
        Assert.Null(outboxMessage.ClaimedUntilUtc);
        Assert.Null(outboxMessage.NextAttemptOnUtc);
        Assert.Null(outboxMessage.FailedOnUtc);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldClearDomainEvents_AfterSuccessfulSave()
    {
        // Arrange
        await using var connection = CreateConnection();

        var serializer = new OutboxMessageSerializer();

        await using var context = CreateContext(
            connection,
            serializer);

        await context.Database.EnsureCreatedAsync();

        var order = new Order(
            new CustomerName("Иван Иванов"),
            Email.Create("ivan@example.com"),
            new Money(100m, "EUR"));

        context.Orders.Add(order);

        order.StartProcessing();

        Assert.Single(order.DomainEvents);

        // Act
        await context.SaveChangesAsync();

        // Assert
        Assert.Empty(order.DomainEvents);
    }


    [Fact]
    public async Task SaveChangesAsync_ShouldCreateCorrectOutboxType_ForProcessingEvent()
    {
        // Arrange
        await using var connection = CreateConnection();

        var serializer = new OutboxMessageSerializer();

        await using var context = CreateContext(
            connection,
            serializer);

        await context.Database.EnsureCreatedAsync();

        var order = new Order(
            new CustomerName("Иван Иванов"),
            Email.Create("ivan@example.com"),
            new Money(100m, "EUR"));

        context.Orders.Add(order);

        order.StartProcessing();

        // Act
        await context.SaveChangesAsync();

        // Assert
        var message = await context.OutboxMessages
            .SingleAsync();

        Assert.Equal(
            typeof(OrderProcessingStartedEvent).AssemblyQualifiedName,
            message.Type);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldCreateMultipleOutboxMessages_WhenMultipleEventsExist()
    {
        // Arrange
        await using var connection = CreateConnection();

        var serializer = new OutboxMessageSerializer();

        await using var context = CreateContext(
            connection,
            serializer);

        await context.Database.EnsureCreatedAsync();

        var order = new Order(
            new CustomerName("Иван Иванов"),
            Email.Create("ivan@example.com"),
            new Money(100m, "EUR"));

        context.Orders.Add(order);

        order.StartProcessing();
        order.Ship();

        Assert.Equal(2, order.DomainEvents.Count);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var messages = await context.OutboxMessages
            .OrderBy(x => x.Id)
            .ToListAsync();

        Assert.Equal(2, messages.Count);

        Assert.Equal(
            typeof(OrderProcessingStartedEvent).AssemblyQualifiedName,
            messages[0].Type);

        Assert.Equal(
            typeof(OrderShippedEvent).AssemblyQualifiedName,
            messages[1].Type);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldNotCreateOutboxMessage_WhenThereAreNoDomainEvents()
    {
        // Arrange
        await using var connection = CreateConnection();

        var serializer = new OutboxMessageSerializer();

        await using var context = CreateContext(
            connection,
            serializer);

        await context.Database.EnsureCreatedAsync();

        var order = new Order(
            new CustomerName("Иван Иванов"),
            Email.Create("ivan@example.com"),
            new Money(100m, "EUR"));

        context.Orders.Add(order);

        // Act
        await context.SaveChangesAsync();

        // Assert
        Assert.Empty(await context.OutboxMessages.ToListAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldCreateCanceledEventInOutbox()
    {
        // Arrange
        await using var connection = CreateConnection();

        var serializer = new OutboxMessageSerializer();

        await using var context = CreateContext(
            connection,
            serializer);

        await context.Database.EnsureCreatedAsync();

        var order = new Order(
            new CustomerName("Иван Иванов"),
            Email.Create("ivan@example.com"),
            new Money(100m, "EUR"));

        context.Orders.Add(order);

        order.Cancel();

        var eventId = order.DomainEvents
            .Single()
            .EventId;

        // Act
        await context.SaveChangesAsync();

        // Assert
        var message = await context.OutboxMessages
            .SingleAsync();

        Assert.Equal(
            typeof(OrderCanceledEvent).AssemblyQualifiedName,
            message.Type);

        Assert.Equal(
            eventId,
            message.EventId);

        Assert.Empty(order.DomainEvents);
    }
}