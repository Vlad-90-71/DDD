using System.Text.Json;
using DDD.Domain.Common.SmartEnum.Json;
using DDD.Domain.Common.ValueObjects.Json;

namespace DDD.Domain;

public static class DomainJsonExtensions
{
    public static JsonSerializerOptions AddDomainJsonConverters(this JsonSerializerOptions options)
    {
        options.Converters.Add(new EmailJsonConverter());
        options.Converters.Add(new CustomerNameJsonConverter());
        options.Converters.Add(new MoneyJsonConverter());
        options.Converters.Add(new SmartEnumJsonConverterFactory());

        return options;
    }
}