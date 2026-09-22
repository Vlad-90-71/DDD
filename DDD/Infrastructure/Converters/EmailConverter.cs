using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DDD.Domain.Common.ValueObjects;

namespace DDD.Infrastructure.Converters;

public sealed class EmailConverter : ValueConverter<Email, string>
{
    public EmailConverter()
        : base(
            email => email.Value,
            value => Email.Create(value))
    {
    }
}