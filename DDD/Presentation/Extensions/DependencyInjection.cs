using DDD.Application.Common;
using DDD.Domain.Common.SmartEnum;
using DDD.Domain.Common.SmartEnum.Json;
using DDD.Presentation.Exceptions;
using System.Reflection;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class DependencyInjection
{
    public static IServiceCollection AddPresentationControllers(this IServiceCollection services)
    {
        // Регистрируем службы поддержки ProblemDetails и наш обработчик
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddControllers(options =>
        {
            options.Filters.Add<FluentValidationFilter>();
        })
            .AddJsonOptions(options =>
            {
                // Подключаем глобальную фабрику для всех SmartEnum
                options.JsonSerializerOptions.Converters.Add(new SmartEnumJsonConverterFactory());
            });

        return services;
    }

    public static IServiceCollection AddApplicationOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(static options =>
        {
            options.AddSchemaTransformer(static (schema, context, cancellationToken) =>
            {
                var type = context.JsonTypeInfo.Type;

                if (!type.IsClass)
                    return Task.CompletedTask;

                var smartEnumInterface = type.GetInterfaces()
                    .FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISmartEnum<>));

                if (smartEnumInterface != null)
                {
                    // Находим метод получения готовой строки
                    var getDescriptionMethod = type.GetMethod("GetOpenApiDescription",
                            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                    // Invoke возвращает object, содержащий ссылку на string. Боксинга структуры НЕ происходит.
                    if (getDescriptionMethod?.Invoke(null, null) is string description)
                    {
                        schema.Type = "integer";
                        schema.Format = "int32";
                        schema.Description = description;

                        schema.Properties?.Clear();
                    }
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}

