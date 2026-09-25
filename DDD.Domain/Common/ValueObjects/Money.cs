using System.Collections.Frozen;

namespace DDD.Domain.Common.ValueObjects;

public sealed class Money : IEquatable<Money>
{
    private static readonly FrozenSet<string> Currencies =
        ["USD", "EUR", "RUB"];

    private static readonly string InlineCurrencies =
        string.Join(", ", Currencies);

    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException(
                $"Сумма не может быть отрицательной. Получено: {amount}.",
                nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException(
                "Валюта не может быть пустой.",
                nameof(currency));

        currency = currency.Trim().ToUpperInvariant();

        if (!Currencies.Contains(currency))
            throw new ArgumentException(
                $"Валюта '{currency}' не поддерживается. " +
                $"Допустимые валюты: [{InlineCurrencies}].",
                nameof(currency));

        Amount = amount;
        Currency = currency;
    }
    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        EnsureSameCurrency(other);

        return new Money(Amount + other.Amount, Currency);
    }
    public Money Subtract(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        EnsureSameCurrency(other);

        var amount = Amount - other.Amount;

        if (amount < 0)
            throw new InvalidOperationException(
                $"Нельзя вычесть {other} из {this}: результат не может быть отрицательным.");

        return new Money(amount, Currency);
    }
    public Money Multiply(decimal multiplier)
    {
        if (multiplier < 0)
            throw new ArgumentException(
                $"Множитель не может быть отрицательным. Получено: {multiplier}.",
                nameof(multiplier));

        return new Money(Amount * multiplier, Currency);
    }
    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException(
                $"Нельзя выполнить операцию с разными валютами: " +
                $"{Currency} и {other.Currency}.");
    }
    public static Money operator +(Money left, Money right) =>
        left.Add(right);

    public static Money operator -(Money left, Money right) =>
        left.Subtract(right);

    public static Money operator *(Money money, decimal multiplier) =>
        money.Multiply(multiplier);

    public static Money operator *(decimal multiplier, Money money) =>
        money.Multiply(multiplier);
    
    public bool Equals(Money? other) =>
        other is not null && Amount == other.Amount && Currency == other.Currency;

    public override bool Equals(object? obj) =>
        obj is Money other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Amount, Currency);

    public static bool operator ==(Money? left, Money? right) =>
        Equals(left, right);

    public static bool operator !=(Money? left, Money? right) =>
        !Equals(left, right);
}