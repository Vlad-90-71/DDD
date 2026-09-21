using Microsoft.EntityFrameworkCore;
using DDD.Domain.Common.SmartEnum;
using DDD.Infrastructure.Converters;

namespace DDD.Infrastructure.Extensions;

public static class SmartEnumModelConfigurationBuilder
{
    /// <summary>
    /// Автоматически находит и настраивает ВСЕ свойства типа SmartEnum (включая Nullable) во всей системе.
    /// </summary>
    public static ModelConfigurationBuilder SmartEnumConfiguration(this ModelConfigurationBuilder configurationBuilder)
    {
        // 1. Регистрируем открытый generic-конвертер для ВСЕХ стандартных SmartEnum свойств
        configurationBuilder
            .Properties(typeof(SmartEnum<>))
            .HaveConversion(typeof(SmartEnumConverter<>))
            .HaveColumnType("int");

        // 2. Регистрируем открытый generic-конвертер для ВСЕХ Nullable SmartEnum свойств
        // EF Core автоматически сопоставит Nullable<T> с NullableSmartEnumConverter<T>
        configurationBuilder
            .Properties(typeof(SmartEnum<>))
            .HaveConversion(typeof(NullableSmartEnumConverter<>))
            .HaveColumnType("int");

        return configurationBuilder;
    }
}
