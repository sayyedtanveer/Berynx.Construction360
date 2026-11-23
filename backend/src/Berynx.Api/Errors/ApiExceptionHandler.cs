using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Berynx.Api.Errors;

public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var isAccessDenied = exception is UnauthorizedAccessException;
        if (isAccessDenied)
        {
            logger.LogWarning(
                "Tenant-scoped request was denied for trace {TraceId}",
                httpContext.TraceIdentifier);
        }
        else
        {
            logger.LogError(
                exception,
                "Unhandled API exception for trace {TraceId}",
                httpContext.TraceIdentifier);
        }

        var statusCode = isAccessDenied
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status500InternalServerError;
        httpContext.Response.StatusCode = statusCode;
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = isAccessDenied
                ? "Access denied."
                : "The request could not be completed.",
            Instance = httpContext.Request.Path,
        };
        problemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails,
            },
            cancellationToken);
    }
}
