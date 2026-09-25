using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DDD.Domain.Common.SmartEnum;
using DDD.Infrastructure.Converters;

namespace DDD.Infrastructure.Configurations;

public static class SmartEnumModelConfigurationBuilder
{
    public static void SmartEnumConfiguration(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var propertyType = property.ClrType;

                if (!SmartEnum.IsSmartEnum(propertyType))
                    continue;

                var converterType = typeof(SmartEnumConverter<>).MakeGenericType(propertyType);
                var converter = (ValueConverter)Activator.CreateInstance(converterType)!;

                property.SetValueConverter(converter);
                property.SetColumnType("int");
            }
        }
    }
}

