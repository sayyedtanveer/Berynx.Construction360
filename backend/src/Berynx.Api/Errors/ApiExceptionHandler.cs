using System.Diagnostics;
using Berynx.Api.IdentityAccess;
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
        var isAccessFailure = exception is UnauthorizedAccessException;
        if (isAccessFailure)
        {
            logger.LogWarning(
                "Authentication or authorization failed for trace {TraceId}",
                httpContext.TraceIdentifier);
        }
        else
        {
            logger.LogError(
                exception,
                "Unhandled API exception for trace {TraceId}",
                httpContext.TraceIdentifier);
        }

        var isAuthenticationEndpoint = httpContext.Request.Path.StartsWithSegments("/api/v1/m02/auth");
        var statusCode = StatusCodeFor(
            exception,
            isAuthenticationEndpoint,
            httpContext.User.Identity?.IsAuthenticated == true);
        httpContext.Response.StatusCode = statusCode;
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Detail = statusCode switch
            {
                400 => exception.Message,
                429 => exception.Message,
                409 => "The record changed or violates a protected business constraint. Reload and retry.",
                _ => null
            },
            Title = isAccessFailure
                ? statusCode == 401 ? "Authentication required." : "Access denied."
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
            });
    }

    public static int StatusCodeFor(
        Exception exception,
        bool isAuthenticationEndpoint,
        bool isAuthenticated) =>
        exception switch
        {
            UnauthorizedAccessException => isAuthenticationEndpoint || !isAuthenticated ? 401 : 403,
            Berynx.Api.Onboarding.TenantApiQuotaExceededException => 429,
            KeyNotFoundException => 404,
            System.ComponentModel.DataAnnotations.ValidationException or System.Text.Json.JsonException or InvalidDataException => 400,
            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => 409,
            Microsoft.EntityFrameworkCore.DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: "23505" } } => 409,
            Microsoft.EntityFrameworkCore.DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: "23503" or "P0001" } } => 409,
            M02ProviderUnavailableException or HttpRequestException or TaskCanceledException => 503,
            _ => 500
        };
}
