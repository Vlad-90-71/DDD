using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DDD.Domain.Common.Events;
using DDD.Domain.Entities.Order;
using DDD.Infrastructure.Events;
using DDD.Infrastructure.Outbox;
using DDD.Infrastructure.Configurations;

namespace DDD.Infrastructure;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IOutboxMessageSerializer outboxMessageSerializer,
    ILogger<AppDbContext> logger)
    : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<EventProcessingState> EventProcessingStates => Set<EventProcessingState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.SmartEnumConfiguration();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<IEntityEvent>().ToArray();

        if (entries.Length > 0)
        {
            logger.LogDebug(
                "Found {EntityCount} entity/entities with domain events.",
                entries.Length);

            var domainEvents = entries.SelectMany(x => x.Entity.DomainEvents).ToArray();

            if (domainEvents.Length > 0)
            {
                logger.LogDebug(
                    "Preparing {DomainEventCount} domain event(s) for persistence. Types={EventTypes}",
                    domainEvents.Length, string.Join(", ", domainEvents.Select(x => x.GetType().Name)));

                OutboxMessages.AddRange(
                    domainEvents.Select(ev => outboxMessageSerializer.Serialize(ev)));

                EventProcessingStates.AddRange(
                    domainEvents.Select(ev => new EventProcessingState(ev.EventId, DateTime.UtcNow)));

                logger.LogDebug(
                    "Prepared {OutboxMessageCount} outbox message(s) and {ProcessingStateCount} event processing state(s).",
                    domainEvents.Length, domainEvents.Length);
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entry in entries)
            entry.Entity.ClearDomainEvents();

        return result;
    }
}