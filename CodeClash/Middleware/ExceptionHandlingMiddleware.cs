using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using CodeClash.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodeClash.API.Middleware;

/// <summary>
/// Global exception handler — catches FluentValidation.ValidationException
/// and all unhandled exceptions, returning standard ProblemDetails.
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
            var problem = new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Validation failed",
                Detail = "One or more validation errors occurred."
            };
            problem.Extensions["errors"] = errors;
            await WriteResponse(context, HttpStatusCode.BadRequest, problem);
        }
        catch (UnauthorizedAccessException ex)
        {
            var problem = new ProblemDetails
            {
                Status = (int)HttpStatusCode.Unauthorized,
                Title = "Unauthorized",
                Detail = ex.Message
            };
            await WriteResponse(context, HttpStatusCode.Unauthorized, problem);
        }
        catch (InvalidOperationException ex)
        {
            var problem = new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Bad Request",
                Detail = ex.Message
            };
            await WriteResponse(context, HttpStatusCode.BadRequest, problem);
        }
        catch (KeyNotFoundException ex)
        {
            var problem = new ProblemDetails
            {
                Status = (int)HttpStatusCode.NotFound,
                Title = "Not Found",
                Detail = ex.Message
            };
            await WriteResponse(context, HttpStatusCode.NotFound, problem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");

            try
            {
                var loggingService = context.RequestServices.GetService<ISystemLoggingService>();
                if (loggingService != null)
                {
                    var isDbError = ex.GetType().FullName?.Contains("EntityFrameworkCore") == true || 
                                    ex.GetType().Name.Contains("Sql") || 
                                    ex.InnerException?.GetType().Name.Contains("Sql") == true;
                    
                    var category = isDbError ? "DATABASE" : "SYSTEM";
                    await loggingService.LogErrorAsync(category, "Global exception handler caught unhandled exception.", nameof(ExceptionHandlingMiddleware), ex);
                }
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Failed to write unhandled exception to system log database");
            }

            var problem = new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Internal Server Error",
                Detail = ex.Message + " | " + ex.InnerException?.Message
            };
            await WriteResponse(context, HttpStatusCode.InternalServerError, problem);
        }
    }

    private static async Task WriteResponse(HttpContext context, HttpStatusCode statusCode, ProblemDetails response)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}