using CodeClash.API.Common;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace CodeClash.API.Middleware;

/// <summary>
/// Global exception handler — catches FluentValidation.ValidationException
/// and all unhandled exceptions, returning a consistent ApiResponse envelope.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors.Select(e => e.ErrorMessage).Distinct().ToList();
            await WriteResponse(context, HttpStatusCode.BadRequest,
                ApiResponse<object>.Fail(errors, "Validation failed"));
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteResponse(context, HttpStatusCode.Unauthorized,
                ApiResponse<object>.Fail(ex.Message, "Unauthorized"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await WriteResponse(context, HttpStatusCode.InternalServerError,
                ApiResponse<object>.Fail("An unexpected error occurred. Please try again later."));
        }
    }

    private static async Task WriteResponse<T>(HttpContext context, HttpStatusCode statusCode, ApiResponse<T> response)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}