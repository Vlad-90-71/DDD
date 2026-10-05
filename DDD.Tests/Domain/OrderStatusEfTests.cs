using DDD.Domain.Common.ValueObjects;
using DDD.Domain.Entities.Order;
using DDD.Domain.Enums;
using DDD.Infrastructure;
using DDD.Infrastructure.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DDD.Tests.Domain;

public class OrderStatusEfTests
{
    [Fact]
    public async Task Should_Save_And_Read_OrderStatus()
    {
        await using var connection =
            new SqliteConnection("DataSource=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var serializer = new OutboxMessageSerializer();

        await using (var context = new AppDbContext(options, serializer))
        {
            await context.Database.EnsureCreatedAsync();

            var order = CreateOrder();

            context.Orders.Add(order);

            await context.SaveChangesAsync();
        }

        await using (var context = new AppDbContext(options, serializer))
        {
            var order = await context.Orders
                .SingleAsync();

            Assert.Equal(OrderStatus.New, order.Status);
        }
    }

    private static Order CreateOrder()
    {
        return new Order(
            "Адрес ул 55",
            new CustomerName("Иван Иванов"),
            Email.Create("ivan@example.com"),
            new Money(100, "EUR"));
    }
}