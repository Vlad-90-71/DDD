using DDD.Application.Common;
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
        services.AddSingleton<IFailureSimulator, FailureSimulator>();

        // Регистрируем наш сервис
        services.AddScoped<IOrderService, OrderService>();

        services.AddDomainEventHandler<OrderProcessingStartedEvent, OrderProcessingStartedEventHandler>();
        services.AddDomainEventHandler<OrderShippedEvent, OrderShippedEventHandler>();
        services.AddDomainEventHandler<OrderCanceledEvent, OrderCanceledEventHandler>();

        services.AddScoped<IOutboxService, OutboxService>();

        // Автоматически находит и регистрирует ВСЕ валидаторы FluentValidation в этой сборке
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
