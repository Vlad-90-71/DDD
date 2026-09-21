using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using FluentValidation;

namespace DDD.Presentation.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. Логируем критическую ошибку с трассировкой стека для разработчиков
        logger.LogError(exception, "Произошло необработанная ошибка приложения: {Message}", exception.Message);

        // 2. Определяем HTTP-статус и наполнение ответа в зависимости от типа исключения
        var problemDetails = exception switch
        {
            // Сценарий А: Ошибки бизнес-валидации от FluentValidation
            ValidationException validationEx => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка валидации данных",
                Detail = "Один или несколько параметров запроса не прошли проверку.",
                Type = "https://ietf.org",
                Extensions = new Dictionary<string, object?>
                {
                    // Группируем ошибки по полям: "StatusId": ["Сообщение 1", "Сообщение 2"]
                    { "errors", validationEx.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(
                            static g => g.Key,
                            static g => g.Select(static x => x.ErrorMessage).ToArray()
                        )
                    }
                }
            },

            // Сценарий Б: Ошибка десериализации (например, клиент передал кривой JSON 
            // или SmartEnumJsonConverter выбросил JsonException для невалидного энама)
            JsonException jsonEx => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка чтения тела запроса (Bad Request)",
                Detail = jsonEx.Message, // Сюда попадает ваш красивый текст: "Значение '99' невалидно..."
                Type = "https://ietf.org"
            },

            // Сценарий В: Перехват ошибок кастинга (если FromValue выбросил InvalidCastException)
            InvalidCastException castEx when castEx.Message.Contains("невалидно для") => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Некорректное значение перечисления",
                Detail = castEx.Message,
                Type = "https://ietf.org"
            },

            // Сценарий Г: Все остальные непредвиденные ошибки (база данных упала, NullReferenceException и т.д.)
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Внутренняя ошибка сервера",
                Detail = "На стороне сервера произошла непредвиденная ошибка. Пожалуйста, обратитесь в техническую поддержку.",
                Type = "https://ietf.org"
            }
        };

        // 3. Настраиваем метаданные HTTP-ответа
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        // 4. Записываем структурированный JSON-ответ напрямую в сетевой поток
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // Возвращаем true, чтобы ASP.NET Core знал, что ошибка успешно обработана и конвейер закрыт
        return true;
    }
}
