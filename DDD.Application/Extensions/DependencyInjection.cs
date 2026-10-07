using DDD.Application.Common;
using DDD.Application.Common.Events;
using DDD.Application.Services.OrderService;
using DDD.Domain.Common;
using DDD.Domain.Entities.Order;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DDD.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddSingleton<IFailureSimulator, FailureSimulator>();

        services.AddScoped<IOrderService, OrderService>();

        // Events without business processing
        services.AddDomainEventHandler<OrderCreatedEvent>();
        services.AddDomainEventHandler<OrderProcessingStartedEvent>();
        services.AddDomainEventHandler<OrderCanceledEvent>();

        // Events with business processing
        services.AddDomainEventHandler<OrderProcessingRequestedEvent, OrderProcessingRequestedEventHandler>();
        services.AddDomainEventHandler<OrderShippedEvent, OrderShippedEventHandler>();

        // Validators
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}