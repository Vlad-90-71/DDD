using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DDD.Domain.Common;
using DDD.Infrastructure.Persistence.Converters;

namespace DDD.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // АВТОМАТИЧЕСКАЯ НАСТРОЙКА ВСЕХ SMART ENUM В СИСТЕМЕ
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var smartEnumProperties = entityType.ClrType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISmartEnum<>)));

            foreach (var property in smartEnumProperties)
            {
                var enumType = property.PropertyType;
                var converterType = typeof(SmartEnumConverter<>).MakeGenericType(enumType);
                var converterInstance = (ValueConverter)Activator.CreateInstance(converterType)!;

                modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name)
                    .HasConversion(converterInstance);
            }
        }
    }
}
