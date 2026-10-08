using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DDD.Domain.Common.SmartEnum;
using DDD.Infrastructure.Converters;

namespace DDD.Infrastructure.Configurations;

public static class SmartEnumModelConfigurationBuilder
{
    public static void SmartEnumConfiguration(
        this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var propertyType = property.ClrType;

                var underlyingType = Nullable.GetUnderlyingType(propertyType);

                var smartEnumType = underlyingType ?? propertyType;

                if (!SmartEnum.IsSmartEnum(smartEnumType))
                    continue;

                var converterType = underlyingType is not null
                    ? typeof(NullableSmartEnumConverter<>).MakeGenericType(smartEnumType)
                    : typeof(SmartEnumConverter<>).MakeGenericType(smartEnumType);

                var converter = (ValueConverter)Activator.CreateInstance(converterType)!;

                property.SetValueConverter(converter);
                property.SetColumnType("int");
            }
        }
    }
}