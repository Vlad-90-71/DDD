using FluentValidation;
using DDD.Domain.Common;

namespace DDD.Application.Common;

public static class SmartEnumValidationExtensions
{
    // Валидация для свойств типа int (проверяет, существует ли такой Id в SmartEnum)
    public static IRuleBuilderOptions<T, int> IsInSmartEnum<T, TEnum>(this IRuleBuilder<T, int> ruleBuilder)
        where TEnum : ISmartEnum<TEnum>
    {
        var validIds = TEnum.GetAll().Select(static x => x.Value).ToHashSet();

        return ruleBuilder
            .Must(value => validIds.Contains(value))
            .WithMessage((_, value) => $"Значение '{value}' невалидно для {typeof(TEnum).Name}. " +
                                       $"Допустимые значения: {string.Join(", ", validIds)}.");
    }
}
