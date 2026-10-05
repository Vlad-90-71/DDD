using Microsoft.EntityFrameworkCore;
using DDD.Domain.Common.Events;
using DDD.Infrastructure.Outbox;
using DDD.Infrastructure.Configurations;
using DDD.Infrastructure.Events;
using DDD.Domain.Entities.Order;

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

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);

        modelBuilder.SmartEnumConfiguration();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<IEntityEvent>().ToArray();

        if (entries.Length > 0)
        {
            var domainEvents = entries.SelectMany(x => x.Entity.DomainEvents).ToArray();

            if (domainEvents.Length > 0)
            {
                OutboxMessages.AddRange(domainEvents.Select(ev => outboxMessageSerializer.Serialize(ev)));

                EventLogs.AddRange(domainEvents.Select(ev => new EventLog(ev)));
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entry in entries)
        {
            entry.Entity.ClearDomainEvents();
        }

        return result;
    }
}