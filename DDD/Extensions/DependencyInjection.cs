using DDD.Domain.Common.SmartEnum.Json;
using DDD.OpenApi;
using DDD.Exceptions;


namespace DDD.Extensions;

public static partial class DependencyInjection
{
    public static IServiceCollection AddPresentationControllers(this IServiceCollection services)
    {
        services.AddProblemDetails();

        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Один экземпляр Factory можно использовать в обоих JSON pipeline.
        var smartEnumFactory = new SmartEnumJsonConverterFactory();

        // Minimal API / Http.Json
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(smartEnumFactory));

        // MVC / Controllers
        services
            .AddControllers(options => options.Filters.Add<FluentValidationFilter>())
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(smartEnumFactory));

        services.AddOpenApi(options => options.AddSchemaTransformer<SmartEnumSchemaTransformer>());

        return services;
    }


}