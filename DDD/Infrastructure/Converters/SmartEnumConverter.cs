using System.Reflection;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DDD.Domain.Common.SmartEnum;

namespace DDD.Infrastructure.Converters;

public class SmartEnumConverter<T> : ValueConverter<T, int> where T : SmartEnum<T>, ISmartEnum<T>
{
    public SmartEnumConverter() : base(
        static v => v.Value,
        CreateDeserializationExpression()
    )
    { }

    private static Expression<Func<int, T>> CreateDeserializationExpression()
    {
        // Находим метод FromValue через рефлексию
        var method = typeof(T).GetMethod("FromValue", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy, [typeof(int)])
            ?? throw new InvalidOperationException($"Метод FromValue не найден в типе {typeof(T).Name}");

        var parameter = Expression.Parameter(typeof(int), "id");
        var call = Expression.Call(method, parameter);

        return Expression.Lambda<Func<int, T>>(call, parameter);
    }
}

