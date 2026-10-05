using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using DDD.Domain.Entities.Order;
using DDD.Application.Common;
using DDD.Application.Common.Events;
using DDD.Application.Services.OrderService;

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
        services.AddDomainEventHandler<OrderShippedEvent>();
        services.AddDomainEventHandler<OrderCanceledEvent>();

        // Events with business processing
        services.AddDomainEventHandler<OrderProcessingRequestedEvent, OrderProcessingRequestedEventHandler>();

        // Validators
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}