using FluentValidation;
using DDD.Domain.Common.SmartEnum;

namespace DDD.Application.Common;

public static class SmartEnumValidation
{
    public static IRuleBuilderOptions<T, int> IsInSmartEnum<T, TEnum>(this IRuleBuilder<T, int> ruleBuilder)
        where TEnum : SmartEnum<TEnum>
    {
        return ruleBuilder
            .Must((_, value, context) =>
            {
                if (SmartEnum<TEnum>.TryParse(value, out TEnum? _)) return true;

                var errorMessage = SmartEnum<TEnum>.GetInvalidValueMessage(value);
                context.MessageFormatter.AppendArgument("SmartEnumError", errorMessage);

                return false;
            })
            .WithMessage("{SmartEnumError}");
    }
}
