using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using FluentValidation;
using DDD.Application.Orders;

namespace DDD.Presentation.Exceptions;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Произошло необработанное исключение: {Message}",
            exception.Message);

        var problemDetails = exception switch
        {
            ValidationException ex => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка валидации данных",
                Detail = "Один или несколько параметров запроса не прошли проверку.",
                Type = "https://ietf.org",
                Extensions = new Dictionary<string, object?>
                {
                    ["errors"] = ex.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(
                            static g => g.Key,
                            static g => g
                                .Select(static x => x.ErrorMessage)
                                .ToArray())
                }
            },

            EmailAlreadyUsedException ex => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Email уже используется",
                Detail = ex.Message,
                Type = "https://ietf.org"
            },

            JsonException ex => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка чтения данных",
                Detail = ex.Message,
                Type = "https://ietf.org"
            },

            ArgumentNullException ex => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Обязательное значение не указано",
                Detail = ex.Message,
                Type = "https://ietf.org"
            },

            ArgumentException ex => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Некорректные данные",
                Detail = ex.Message,
                Type = "https://ietf.org"
            },

            InvalidOperationException ex => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Недопустимая операция",
                Detail = ex.Message,
                Type = "https://ietf.org"
            },

            InvalidCastException ex when ex.Message.Contains("невалидно для") =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Некорректное значение",
                    Detail = ex.Message,
                    Type = "https://ietf.org"
                },

            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Внутренняя ошибка сервера",
                Detail = "На стороне сервера произошла непредвиденная ошибка. Пожалуйста, обратитесь в техническую поддержку.",
                Type = "https://ietf.org"
            }
        };

        httpContext.Response.StatusCode =
            problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }
}