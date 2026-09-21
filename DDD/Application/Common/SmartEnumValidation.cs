using FluentValidation;
using DDD.Domain.Common.SmartEnum;

namespace DDD.Application.Common;

public static class SmartEnumValidation
{
    /// <summary>
    /// Валидация обязательного числового поля для SmartEnum.
    /// </summary>
    public static IRuleBuilderOptionsConditions<T, int> IsInSmartEnum<T, TEnum>(this IRuleBuilder<T, int> ruleBuilder)
        where TEnum : SmartEnum<TEnum>
    {
        return ruleBuilder.Custom(static (value, context) =>
        {
            if (!SmartEnum<TEnum>.TryParse(value, out _))
            {
                var errorMessage = SmartEnum<TEnum>.GetInvalidValueMessage(value);
                context.FailMessage(errorMessage);
            }
        });
    }

    /// <summary>
    /// Валидация необязательного (Nullable) числового поля для SmartEnum.
    /// </summary>
    public static IRuleBuilderOptionsConditions<T, int?> IsInSmartEnum<T, TEnum>(this IRuleBuilder<T, int?> ruleBuilder)
        where TEnum : SmartEnum<TEnum>
    {
        return ruleBuilder.Custom(static (value, context) =>
        {
            if (value is null)
                return;

            if (!SmartEnum<TEnum>.TryParse(value.Value, out _))
            {
                var errorMessage = SmartEnum<TEnum>.GetInvalidValueMessage(value.Value);
                context.FailMessage(errorMessage);
            }
        });
    }

    private static void FailMessage<T>(this ValidationContext<T> context, string message)
    {
        context.MessageFormatter.AppendArgument("SmartEnumError", message);
        context.AddFailure("{SmartEnumError}");
    }
}
