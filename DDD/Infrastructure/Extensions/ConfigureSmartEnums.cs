using Microsoft.EntityFrameworkCore;
using DDD.Domain.Common.SmartEnum;
using DDD.Infrastructure.Converters;

namespace DDD.Infrastructure.Extensions;

public static class SmartEnumModelConfigurationBuilderExtensions
{
    /// <summary>
    /// Автоматически находит и настраивает все свойства типа SmartEnum во всей системе.
    /// </summary>
    public static ModelConfigurationBuilder ConfigureSmartEnums(this ModelConfigurationBuilder configurationBuilder)
    {
        // 1. Сканируем сборку Домена и находим все конкретные (не абстрактные) классы SmartEnum
        var smartEnumTypes = typeof(SmartEnum<>).Assembly.GetTypes()
            .Where(static t => !t.IsAbstract &&
                               t.BaseType is { IsGenericType: true } &&
                               t.BaseType.GetGenericTypeDefinition() == typeof(SmartEnum<>));

        // 2. Регистрируем наш конвертер для каждого найденного типа энама
        foreach (var enumType in smartEnumTypes)
        {
            // Строим ТИП закрытого конвертера, например: typeof(SmartEnumConverter<OrderStatus>)
            var converterType = typeof(SmartEnumConverter<>).MakeGenericType(enumType);

            configurationBuilder.Properties(enumType).HaveConversion(converterType);
        }

        return configurationBuilder;
    }
}
