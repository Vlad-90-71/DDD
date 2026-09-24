using Microsoft.EntityFrameworkCore;
using DDD.Domain.Common;
using DDD.Domain.Entities;
using DDD.Infrastructure.Outbox;
using DDD.Infrastructure.Extensions;
using DDD.Infrastructure.Log;

namespace DDD.Infrastructure;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IOutboxMessageSerializer outboxMessageSerializer) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    public DbSet<EventLog> EventLogs => Set<EventLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.SmartEnumConfiguration(); 
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker
            .Entries()
            .Select(x => x.Entity)
            .OfType<IAggregateRoot>()
            .ToList();

        var domainEvents = aggregates
            .SelectMany(x => x.DomainEvents)
            .ToList();

        foreach (var domainEvent in domainEvents)
        {
            OutboxMessages.Add(outboxMessageSerializer.Serialize(domainEvent));
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        return result;
    }
}