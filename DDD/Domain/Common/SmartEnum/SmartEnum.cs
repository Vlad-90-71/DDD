using System.Reflection;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace DDD.Domain.Common.SmartEnum;

public record struct SmartEnumRawItem(int Value, string Name, string Display);

public abstract class SmartEnum<T>(int value, string displayName) : ISmartEnum<T>, IEquatable<SmartEnum<T>>
    where T : SmartEnum<T>
{
    // Единый контейнер для всех статических данных, связанных с типом T
    private record struct EnumMetadata(
        FrozenDictionary<string, T> Names,
        FrozenDictionary<string, T> Displays,
        FrozenDictionary<int, T> Values,
        IReadOnlyCollection<SmartEnumRawItem> AllowedValuesList,
        string InlineValues);

    // Этот Lazy отработает ОДИН раз для каждого класса-наследника.
    // Как только метод выполнится, локальный список 'fields' удалится из памяти навсегда.
    private static readonly Lazy<EnumMetadata> Metadata = new(static () =>
    {
        var fields = typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(static f => f.FieldType == typeof(T))
            .Select(static f => (FieldName: f.Name, Instance: (T)f.GetValue(null)!))
            .ToArray();

        var names = fields.ToFrozenDictionary(static x => x.FieldName, static x => x.Instance, StringComparer.OrdinalIgnoreCase);
        var displays = fields.ToFrozenDictionary(static x => x.Instance.ToString(), static x => x.Instance, StringComparer.OrdinalIgnoreCase);
        var values = fields.ToFrozenDictionary(static x => x.Instance.Value, static x => x.Instance);

        var allowedValuesList = fields
                .Select(static x => new SmartEnumRawItem(x.Instance.Value, x.FieldName, x.Instance.ToString()))
                .ToArray()
                .AsReadOnly();
        
        var inlineValues = string.Join(", ", allowedValuesList.Select(static x => $"{x.Value} - {x.Name} ({x.Display})"));

        return new EnumMetadata(names, displays, values, allowedValuesList, inlineValues);
    });

    public int Value { get; } = value;
    private string DisplayName { get; } = displayName;

    public override string ToString() => DisplayName;

    public static IEnumerable<T> GetAll() => Metadata.Value.Values.Values;

    public static IReadOnlyCollection<SmartEnumRawItem> GetRawAllowedValues() =>
        Metadata.Value.AllowedValuesList;

    public static string GetInvalidValueMessage(object? value) =>
        $"Значение '{value}' невалидно для {typeof(T).Name}. Допустимые варианты: [{Metadata.Value.InlineValues}].";

    public static T FromValue(int value) =>
        Metadata.Value.Values.TryGetValue(value, out var result)
            ? result
            : throw new InvalidCastException(GetInvalidValueMessage(value));

    public static bool TryParse(int value, [NotNullWhen(true)] out T? result) =>
        Metadata.Value.Values.TryGetValue(value, out result);

    public static bool TryParse(string? name, [NotNullWhen(true)] out T? result)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            result = null;
            return false;
        }
        return Metadata.Value.Names.TryGetValue(name, out result);
    }

    public static bool TryParseByDisplay(string? displayName, [NotNullWhen(true)] out T? result)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            result = null;
            return false;
        }
        return Metadata.Value.Displays.TryGetValue(displayName, out result);
    }

    public bool Equals(SmartEnum<T>? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is SmartEnum<T> other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(SmartEnum<T>? left, SmartEnum<T>? right) => Equals(left, right);
    public static bool operator !=(SmartEnum<T>? left, SmartEnum<T>? right) => !Equals(left, right);
}
