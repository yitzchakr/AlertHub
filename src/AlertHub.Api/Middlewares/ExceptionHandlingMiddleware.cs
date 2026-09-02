using AlertHub.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace AlertHub.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            });
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound,
                "Resource not found.", ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict,
                "The request conflicts with existing state.", ex.Message);
        }
        catch (DomainException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest,
                "The request could not be completed.", ex.Message);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = ex.Message
            });
        }
    }

    private static Task WriteProblemAsync(
        HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        });
    }
}
