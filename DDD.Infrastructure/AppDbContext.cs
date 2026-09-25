using Microsoft.EntityFrameworkCore;
using DDD.Domain.Entities;
using DDD.Domain.Common.Events;
using DDD.Infrastructure.Outbox;
using DDD.Infrastructure.Configurations;
using DDD.Infrastructure.Events;

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
        var entities = ChangeTracker
            .Entries()
            .Select(x => x.Entity)
            .OfType<IEntityEvent>()
            .ToArray(); 

        if (entities.Length > 0)
        {
            var outboxMessages = entities
                .SelectMany(x => x.DomainEvents)
                .Select(domainEvent => outboxMessageSerializer.Serialize(domainEvent));

            OutboxMessages.AddRange(outboxMessages);
        }

        var result = await base.SaveChangesAsync(cancellationToken);
        
        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }

        return result;
    }
}