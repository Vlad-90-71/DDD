using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using DDD.Domain.Entities;
using DDD.Application.Common;
using DDD.Application.Common.Events;
using DDD.Application.Services;

namespace DDD.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IFailureSimulator, FailureSimulator>();

        // Регистрируем наш сервис
        services.AddScoped<IOrderService, OrderService>();

        services.AddDomainEventHandler<OrderProcessingStartedEvent>();
        services.AddDomainEventHandler<OrderShippedEvent>();
        services.AddDomainEventHandler<OrderCanceledEvent>();

        // Автоматически находит и регистрирует ВСЕ валидаторы FluentValidation в этой сборке
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }

}
