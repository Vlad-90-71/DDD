using FluentValidation;
using DDD.Domain.Common;

namespace DDD.Application.Common;

public static class SmartEnumValidationExtensions
{
    // Универсальная валидация для свойств типа int
    public static IRuleBuilderOptions<T, int> IsInSmartEnum<T, TEnum>(this IRuleBuilder<T, int> ruleBuilder)
        where TEnum : ISmartEnum<TEnum>
    {
        return ruleBuilder
            .Must((_, value, context) =>
            {
                try
                {
                    // Пытаемся вызвать FromValue. Если ID не существует, сработает ваш throw
                    TEnum.FromValue(value);
                    return true;
                }
                catch (InvalidCastException ex)
                {
                    // КЛЮЧЕВОЙ МОМЕНТ: Передаем сообщение из вашего SmartEnum во FluentValidation
                    context.MessageFormatter.AppendArgument("ErrorMessageFromEnum", ex.Message);
                    return false;
                }
            })
            // Подставляем перехваченный текст ошибки в ответ клиенту
            .WithMessage("{ErrorMessageFromEnum}");
    }
}
