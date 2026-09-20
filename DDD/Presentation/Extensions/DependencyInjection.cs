using System.Text;
using System.Reflection;
using DDD.Application.Common;
using DDD.Domain.Common.SmartEnum;
using DDD.Domain.Common.SmartEnum.Json;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class DependencyInjection
{
    public static IServiceCollection AddPresentationControllers(this IServiceCollection services)
    {
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
        services.AddOpenApi(options =>
        {
            options.AddSchemaTransformer((schema, context, cancellationToken) =>
            {
                var type = context.JsonTypeInfo.Type;

                // Проверяем, реализует ли тип интерфейс ISmartEnum<>
                var smartEnumInterface = type.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISmartEnum<>));

                if (smartEnumInterface != null)
                {
                    var getRawValuesMethod = type.GetMethod("GetRawAllowedValues",
                            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                    if (getRawValuesMethod != null && getRawValuesMethod.Invoke(null, null) is IEnumerable<SmartEnumRawItem> rawValues)
                    {
                        var descriptionBuilder = new StringBuilder("Доступные значения:\n");

                        foreach (var item in rawValues)
                        {
                            descriptionBuilder.AppendLine($"* **{item.Value}** — {item.Name} ({item.Display})");
                        }

                        schema.Type = "integer";
                        schema.Format = "int32";
                        schema.Description = descriptionBuilder.ToString().TrimEnd();

                        schema.Properties?.Clear();
                    }
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}

