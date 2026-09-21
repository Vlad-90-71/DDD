namespace DDD.Domain.Common.ValueObjects;

public sealed record CustomerName
{
    public string Value { get; }

    public CustomerName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length > 200)
            throw new ArgumentException(
                "Имя клиента не может содержать более 200 символов.",
                nameof(value));

        Value = value.Trim();
    }

    public override string ToString() => Value;
}