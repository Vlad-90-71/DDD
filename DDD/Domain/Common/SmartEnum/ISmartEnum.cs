using System.Diagnostics.CodeAnalysis;

namespace DDD.Domain.Common.SmartEnum;

public interface ISmartEnum<T> where T : ISmartEnum<T>
{
    int Value { get; }
    string ToString();
    static abstract IEnumerable<T> GetAll();
    static abstract T FromValue(int value);
    static abstract bool TryParse(int value, [NotNullWhen(true)] out T? result);
    static abstract bool TryParse(string? name, [NotNullWhen(true)] out T? result);
}
