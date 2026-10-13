using DDD.MessageBroker.Contracts;
using DDD.MessageBroker.RabbitMQ.Configuration;
using DDD.MessageBroker.RabbitMQ.Connection;
using DDD.MessageBroker.RabbitMQ.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DDD.MessageBroker.RabbitMQ.Extensions;

public static class RabbitMqMessageBrokerExtensions
{
    public static IServiceCollection AddRabbitMqMessageBroker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.HostName),
                "RabbitMQ HostName is required.")
            .Validate(
                options => options.Port is > 0 and <= 65535,
                "RabbitMQ Port is invalid.")
            .ValidateOnStart();

        services.AddSingleton<RabbitMqConnectionManager>();
        services.AddSingleton<RabbitMqTopology>();

        services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

        services.AddHostedService<RabbitMqMessageConsumer>();

        return services;
    }
}