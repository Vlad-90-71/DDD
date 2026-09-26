using Microsoft.Extensions.DependencyInjection;
using DDD.MessageBroker.Contracts;

namespace DDD.MessageBroker.InMemory;

public static class InMemoryMessageBrokerExtensions
{
    public static IServiceCollection AddInMemoryMessageBroker(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryMessageBroker>();
        services.AddSingleton<IMessagePublisher, InMemoryMessagePublisher>();
        services.AddHostedService<InMemoryMessageConsumer>();

        return services;
    }
}