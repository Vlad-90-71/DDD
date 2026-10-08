using DDD.Domain;
using DDD.Domain.Common.SmartEnum.Json;
using DDD.Exceptions;
using DDD.OpenApi;
using Microsoft.Extensions.Options;


namespace DDD.Extensions;

public static partial class DependencyInjection
{
    public static IServiceCollection AddPresentationControllers(this IServiceCollection services)
    {
        services.AddProblemDetails();

        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Minimal API / Http.Json
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.AddDomainJsonConverters());

        // MVC / Controllers
        services
            .AddControllers(options => options.Filters.Add<FluentValidationFilter>())
            .AddJsonOptions(options => options.JsonSerializerOptions.AddDomainJsonConverters());

        services.AddOpenApi(options => options.AddSchemaTransformer<SmartEnumSchemaTransformer>());

        return services;
    }


}