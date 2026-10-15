using Microsoft.Extensions.DependencyInjection;
using DDD.Domain.Common.Events;

namespace DDD.Application.Extensions;

public static class DomainEventHandlerRegistrationExtensions
{
    public static IServiceCollection AddDefaultDomainEventHandler<TEvent>(
        this IServiceCollection services) where TEvent : DomainEvent
    {
        services.AddScoped<DefaultDomainEventHandler<TEvent>>();

        services.AddScoped<IDomainEventHandler<TEvent>>(
            sp => sp.GetRequiredService<DefaultDomainEventHandler<TEvent>>());

        services.AddScoped<IDomainEventHandler>(
            sp => sp.GetRequiredService<DefaultDomainEventHandler<TEvent>>());

        return services;
    }

    public static IServiceCollection AddDomainEventHandler<TEvent, THandler>(
        this IServiceCollection services)
        where TEvent : DomainEvent
        where THandler : DomainEventHandler<TEvent>
    {
        services.AddScoped<THandler>();

        services.AddScoped<IDomainEventHandler<TEvent>>(
            sp => sp.GetRequiredService<THandler>());

        services.AddScoped<IDomainEventHandler>(
            sp => sp.GetRequiredService<THandler>());

        return services;
    }
}