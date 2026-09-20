using DDD.Domain.Common;
using DDD.Presentation.Common.Json;
using System.Reflection;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class DependencyInjection
{
    public static IServiceCollection AddPresentationControllers(this IServiceCollection services)
    {
        services.AddControllers()
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

                var smartEnumInterface = type.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISmartEnum<>));

                if (smartEnumInterface != null)
                {
                    var getAllMethod = type.GetMethod("GetAll",
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                    if (getAllMethod != null && getAllMethod.Invoke(null, null) is System.Collections.IEnumerable values)
                    {
                        var descriptionBuilder = new StringBuilder("Доступные значения:\n");

                        foreach (var item in values)
                        {
                            var valueProp = item.GetType().GetProperty("Value")?.GetValue(item);
                            var name = item.ToString();

                            descriptionBuilder.AppendLine($"* **{valueProp}** = {name}");
                        }

                        schema.Type = "integer";
                        schema.Description = descriptionBuilder.ToString();
                        schema.Properties.Clear();
                    }
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}

