using DDD.Domain.Common.Events;
using DDD.Application.Common.Events;

namespace DDD.Application.Extensions;

public static class DomainEventHandlerRegistrationExtensions
{
    public static IServiceCollection AddDomainEventHandler<TEvent, THandler>(
        this IServiceCollection services)
        where TEvent : IDomainEvent
        where THandler : class, IDomainEventHandler<TEvent>
    {
        services.AddScoped<THandler>();

        services.AddScoped<IDomainEventHandler<TEvent>>(sp => sp.GetRequiredService<THandler>());
        services.AddScoped<IDomainEventHandler>(sp => sp.GetRequiredService<THandler>());

        return services;
    }
}