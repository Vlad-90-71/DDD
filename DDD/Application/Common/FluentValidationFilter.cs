using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DDD.Application.Common;

public class FluentValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Перебираем все аргументы, которые пришли в экшен контроллера (например, CreateOrderDto)
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            // Динамически запрашиваем IValidator<T> из DI для типа этого аргумента
            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

            if (validator is not null)
            {
                // Создаем контекст валидации и запускаем АСИНХРОННУЮ проверку
                var validationContext = new ValidationContext<object>(argument);
                var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

                if (!validationResult.IsValid)
                {
                    // Преобразуем ошибки в стандартный словарь ModelState dictionary
                    var modelState = context.ModelState;
                    foreach (var error in validationResult.Errors)
                    {
                        modelState.AddModelError(error.PropertyName, error.ErrorMessage);
                    }

                    // Возвращаем ValidationProblemDetails (стандартный 400 BadRequest для API)
                    context.Result = new BadRequestObjectResult(new ValidationProblemDetails(modelState));
                    return; // Прерываем выполнение, в экшен контроллера запрос не пойдет
                }
            }
        }

        await next(); // Если всё валидно, передаем управление дальше в контроллер
    }
}
