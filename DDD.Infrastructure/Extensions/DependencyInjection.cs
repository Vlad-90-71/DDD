using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DDD.Eventing.Contracts;
using DDD.Application.Common;
using DDD.Application.Services;
using DDD.Infrastructure.Events;
using DDD.Infrastructure.Outbox;
using DDD.Infrastructure.Repositories;

namespace DDD.Infrastructure.Extensions;

public static partial class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // EF Core / Persistence
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        // Domain Events
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IEventProcessingStore, EventProcessingStore>();

        // Outbox
        services.AddScoped<IOutboxMessageSerializer, OutboxMessageSerializer>();
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddScoped<OutboxProcessor>();

        // Background workers
        services.AddHostedService<OutboxBackgroundService>();

        return services;
    }
}
