using DDD.Application.Common.Events;
using DDD.Application.Extensions;
using DDD.Application.Orders;
using DDD.Application.Orders.Events;
using DDD.Domain.Entities;
using FluentValidation;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Регистрируем наш сервис
        services.AddScoped<IOrderService, OrderService>();
        /*services.AddScoped<IDomainEventHandler<OrderProcessingStartedEvent>, OrderProcessingStartedEventHandler>();
        services.AddScoped<IDomainEventHandler<OrderShippedEvent>, OrderShippedEventHandler>();
        services.AddScoped<IDomainEventHandler<OrderCanceledEvent>, OrderCanceledEventHandler>();
        */
        services.AddDomainEventHandler<OrderProcessingStartedEvent, OrderProcessingStartedEventHandler>();
        services.AddDomainEventHandler<OrderShippedEvent, OrderShippedEventHandler>();
        services.AddDomainEventHandler<OrderCanceledEvent, OrderCanceledEventHandler>();

        // Автоматически находит и регистрирует ВСЕ валидаторы FluentValidation в этой сборке
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
