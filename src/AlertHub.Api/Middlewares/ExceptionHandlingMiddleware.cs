using AlertHub.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AlertHub.Api.Middlewares;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // Once the response has begun, the status line and part of the body are
            // already on the wire, and writing again throws over the top of the original
            // fault. Rethrowing lets the server abort the connection, which is the only
            // honest signal left.
            if (context.Response.HasStarted)
            {
                logger.LogError(ex,
                    "Unhandled exception after the response had started. TraceId: {TraceId}",
                    TraceIdOf(context));
                throw;
            }

            await WriteFailureAsync(context, ex);
        }
    }

    private Task WriteFailureAsync(HttpContext context, Exception exception)
    {
        var traceId = TraceIdOf(context);

        switch (exception)
        {
            case ValidationException validation:
                logger.LogDebug("Validation failed. TraceId: {TraceId}", traceId);

                var errors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                return WriteAsync(context, traceId, new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                });

            // These three carry messages an author wrote for the caller to read, so
            // returning them is deliberate. The leak was never "returning a message",
            // it was returning an unvetted one.
            case NotFoundException:
                return WriteExpectedAsync(context, traceId, exception,
                    StatusCodes.Status404NotFound, "Resource not found.");

            case ConflictException:
                return WriteExpectedAsync(context, traceId, exception,
                    StatusCodes.Status409Conflict, "The request conflicts with existing state.");

            case DomainException:
                return WriteExpectedAsync(context, traceId, exception,
                    StatusCodes.Status400BadRequest, "The request could not be completed.");

            default:
                // The only record of an unexpected fault. Before this, the response body
                // was it — which is exactly why the body could not simply be emptied.
                logger.LogError(exception,
                    "Unhandled exception processing {Method} {Path}. TraceId: {TraceId}",
                    context.Request.Method, context.Request.Path, traceId);

                return WriteAsync(context, traceId, new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred.",
                    // Arbitrary exception text can carry SQL, file paths, or connection
                    // details, so the caller gets a trace id to quote instead.
                    Detail = environment.IsDevelopment() ? exception.ToString() : null
                });
        }
    }

    private Task WriteExpectedAsync(
        HttpContext context, string traceId, Exception exception, int statusCode, string title)
    {
        logger.LogDebug("{Title} TraceId: {TraceId}", title, traceId);

        return WriteAsync(context, traceId, new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message
        });
    }

    // Generic so the concrete problem type is inferred at each call site. Declaring the
    // parameter as ProblemDetails instead would make WriteAsJsonAsync serialize that
    // base type and silently drop ValidationProblemDetails.Errors, costing every
    // validation failure its per-field messages.
    private static Task WriteAsync<TProblem>(HttpContext context, string traceId, TProblem problem)
        where TProblem : ProblemDetails
    {
        problem.Extensions["traceId"] = traceId;
        context.Response.StatusCode = problem.Status!.Value;
        return context.Response.WriteAsJsonAsync(problem);
    }

    // Activity.Current carries the W3C trace id when distributed tracing is on; the
    // per-connection identifier is the fallback.
    private static string TraceIdOf(HttpContext context) =>
        Activity.Current?.Id ?? context.TraceIdentifier;
}
