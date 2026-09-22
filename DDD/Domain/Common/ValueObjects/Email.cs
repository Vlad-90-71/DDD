using System.Net.Mail;

namespace DDD.Domain.Common.ValueObjects;

public sealed class Email : IEquatable<Email>
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Email не может быть пустым.",
                nameof(value));

        value = value.Trim();

        try
        {
            var mailAddress = new MailAddress(value);

            if (mailAddress.Address != value)
                throw new FormatException($"Строка '{value}' не является корректным Email-адресом.");
        }
        catch
        {
            throw new ArgumentException(
                $"Некорректный email: '{value}'.",
                nameof(value));
        }

        return new Email(value);
    }

    public override string ToString() => Value;

    public bool Equals(Email? other) =>
        other is not null &&
        string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) =>
        obj is Email other && Equals(other);

    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public static bool operator ==(Email? left, Email? right) =>
        Equals(left, right);

    public static bool operator !=(Email? left, Email? right) =>
        !Equals(left, right);
}