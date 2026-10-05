using Microsoft.Extensions.DependencyInjection;
using DDD.Domain.Common.Events;
using DDD.Eventing.Contracts;

namespace DDD.Application.Common.Events;

public static class DomainEventHandlerRegistrationExtensions
{
    public static IServiceCollection AddDomainEventHandler<TEvent>(
        this IServiceCollection services) where TEvent : IDomainEvent
    {
        services.AddScoped<DomainEventHandler<TEvent>>();

        services.AddScoped<IDomainEventHandler<TEvent>>(
            sp => sp.GetRequiredService<DomainEventHandler<TEvent>>());

        services.AddScoped<IDomainEventHandler>(
            sp => sp.GetRequiredService<DomainEventHandler<TEvent>>());

        return services;
    }

    public static IServiceCollection AddDomainEventHandler<TEvent, THandler>(
        this IServiceCollection services)
        where TEvent : IDomainEvent
        where THandler : class, IDomainEventHandler<TEvent>
    {
        services.AddScoped<THandler>();

        services.AddScoped<IDomainEventHandler<TEvent>>(
            sp => sp.GetRequiredService<THandler>());

        services.AddScoped<IDomainEventHandler>(
            sp => sp.GetRequiredService<THandler>());

        return services;
    }
}