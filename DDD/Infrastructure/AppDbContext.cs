using Microsoft.EntityFrameworkCore;
using DDD.Domain.Entities;
using DDD.Infrastructure.Extensions;
using DDD.Application.Common.Events;

namespace DDD.Infrastructure;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IDomainEventDispatcher domainEventDispatcher) : DbContext(options)
{
    private readonly IDomainEventDispatcher _domainEventDispatcher =
        domainEventDispatcher;

   public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.SmartEnumConfiguration(); 
    }

    public override async Task<int> SaveChangesAsync(
    CancellationToken cancellationToken = default)
    {
        var domainEvents = ChangeTracker
            .Entries<Order>()
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            await _domainEventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }

        foreach (var entry in ChangeTracker.Entries<Order>())
        {
            entry.Entity.ClearDomainEvents();
        }

        return result;
    }
}