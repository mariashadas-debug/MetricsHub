using MetricsHub.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MetricsHub.Api.ErrorHandling;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            ApplicationValidationException => (
                StatusCodes.Status400BadRequest,
                "Invalid request",
                exception.Message),
            NotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message),
            ConflictException => (
                StatusCodes.Status409Conflict,
                "Request conflict",
                exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "The server could not complete the request.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "An unhandled exception occurred while processing the request.");
        }

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            }
        });
    }
}
