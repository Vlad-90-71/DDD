using DDD.Application.Common;
using DDD.Presentation.OpenApi;
using DDD.Presentation.Exceptions;
using DDD.Domain.Common.SmartEnum.Json;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class DependencyInjection
{
    public static IServiceCollection AddPresentationControllers(
        this IServiceCollection services)
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