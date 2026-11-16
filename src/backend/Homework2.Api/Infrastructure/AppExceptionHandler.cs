using Homework2.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Homework2.Api.Infrastructure;

public sealed class AppExceptionHandler(ILogger<AppExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case AppValidationException validationException:
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsJsonAsync(
                    new ValidationProblemDetails(
                        validationException.Errors.ToDictionary(pair => pair.Key, pair => pair.Value))
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Validation failed"
                    },
                    cancellationToken);
                return true;

            case NotFoundException notFoundException:
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                await httpContext.Response.WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Resource not found",
                        Detail = notFoundException.Message
                    },
                    cancellationToken);
                return true;

            default:
                logger.LogError(exception, "Unhandled exception");
                return false;
        }
    }
}