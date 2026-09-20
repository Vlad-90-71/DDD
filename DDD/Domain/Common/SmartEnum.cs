using System.Reflection;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace DDD.Domain.Common;

public abstract class SmartEnum<T>(int value, string displayName) : ISmartEnum<T>, IEquatable<SmartEnum<T>>
    where T : SmartEnum<T>
{
    private static readonly Lazy<FrozenDictionary<string, T>> ValuesByName =
        new(static () =>
            typeof(T)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(static f => f.FieldType == typeof(T))
                .ToDictionary(
                    static f => f.Name,
                    static f => (T)f.GetValue(null)!,
                    StringComparer.OrdinalIgnoreCase
                )
                .ToFrozenDictionary()
        );

    public int Value { get; } = value;
    private string DisplayName { get; } = displayName;

    public override string ToString() => DisplayName;

    public static IEnumerable<T> GetAll() => ValuesByName.Value.Values;

    public static T FromValue(int value) =>
        ValuesByName.Value.Values.FirstOrDefault(s => s.Value == value)
        ?? throw new InvalidCastException($"Невозможно найти элемент типа {typeof(T).Name} со значением {value}.");

    public static bool TryParse(int value, [NotNullWhen(true)] out T? result)
    {
        result = ValuesByName.Value.Values.FirstOrDefault(s => s.Value == value);
        return result is not null;
    }

    public static bool TryParse(string? name, [NotNullWhen(true)] out T? result)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            result = null;
            return false;
        }
        return ValuesByName.Value.TryGetValue(name, out result);
    }

    public bool Equals(SmartEnum<T>? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is SmartEnum<T> other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(SmartEnum<T>? left, SmartEnum<T>? right) => Equals(left, right);
    public static bool operator !=(SmartEnum<T>? left, SmartEnum<T>? right) => !Equals(left, right);
}
