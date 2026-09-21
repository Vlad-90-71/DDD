using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DDD.Domain.Common.SmartEnum;

namespace DDD.Infrastructure.Converters;

public class SmartEnumConverter<T> : ValueConverter<T, int> where T : SmartEnum<T>, ISmartEnum<T>
{
    public SmartEnumConverter() : base(
        static v => v.Value,
        static id => SmartEnum<T>.FromValue(id)
    )
    { }
}

