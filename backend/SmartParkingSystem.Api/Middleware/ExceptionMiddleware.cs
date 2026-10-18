using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartParkingSystem.Application.Common.Exceptions;

namespace SmartParkingSystem.Api.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions,
/// performs structured logging, and returns standardized, secure JSON error responses.
/// Stack traces and sensitive database information are never exposed in production responses.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = MapException(exception);

        // Structured Logging: Warning for client errors (4xx), Error for server errors (5xx)
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled server error occurred while processing {RequestMethod} {RequestPath}. StatusCode: {StatusCode}, Message: {ErrorMessage}, ExceptionType: {ExceptionType}",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                exception.Message,
                exception.GetType().FullName);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Client error occurred while processing {RequestMethod} {RequestPath}. StatusCode: {StatusCode}, Message: {ErrorMessage}, ExceptionType: {ExceptionType}",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                exception.Message,
                exception.GetType().FullName);
        }

        if (context.Response.HasStarted)
        {
            _logger.LogWarning("The response has already started, cannot write global error response.");
            return;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var errorResponse = new ErrorResponse
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var json = JsonSerializer.Serialize(errorResponse, serializerOptions);
        await context.Response.WriteAsync(json);
    }

    private (int StatusCode, string Message, List<string> Errors) MapException(Exception exception)
    {
        return exception switch
        {
            // 1. Validation Errors (400 Bad Request)
            ValidationException valEx => (
                StatusCodes.Status400BadRequest,
                valEx.Message,
                valEx.Errors.Count > 0 ? valEx.Errors : new List<string>()
            ),
            ArgumentNullException argNullEx => (
                StatusCodes.Status400BadRequest,
                argNullEx.Message,
                new List<string>()
            ),
            ArgumentException argEx => (
                StatusCodes.Status400BadRequest,
                argEx.Message,
                new List<string>()
            ),
            FormatException formatEx => (
                StatusCodes.Status400BadRequest,
                formatEx.Message,
                new List<string>()
            ),

            // 2. Unauthorized Errors (401 Unauthorized)
            UnauthorizedException unauthEx => (
                StatusCodes.Status401Unauthorized,
                unauthEx.Message,
                new List<string>()
            ),

            // 3. Forbidden Errors (403 Forbidden)
            ForbiddenException forbiddenEx => (
                StatusCodes.Status403Forbidden,
                forbiddenEx.Message,
                new List<string>()
            ),

            // 4. UnauthorizedAccessException Handling
            // Differentiates between authentication challenge (401) vs resource authorization (403)
            UnauthorizedAccessException unauthAccessEx => (
                IsForbiddenAccess(unauthAccessEx.Message)
                    ? StatusCodes.Status403Forbidden
                    : StatusCodes.Status401Unauthorized,
                unauthAccessEx.Message,
                new List<string>()
            ),

            // 5. Not Found (404 Not Found)
            NotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                notFoundEx.Message,
                new List<string>()
            ),
            KeyNotFoundException keyNotFoundEx => (
                StatusCodes.Status404NotFound,
                keyNotFoundEx.Message,
                new List<string>()
            ),

            // 6. Business Rule Exceptions (400 Bad Request)
            BusinessRuleException bizEx => (
                StatusCodes.Status400BadRequest,
                bizEx.Message,
                bizEx.Errors.Count > 0 ? bizEx.Errors : new List<string>()
            ),
            InvalidOperationException invOpEx => (
                StatusCodes.Status400BadRequest,
                invOpEx.Message,
                new List<string>()
            ),

            // 7. Database Exceptions
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "A concurrency conflict occurred. The resource was modified by another operation. Please retry.",
                new List<string>()
            ),
            DbUpdateException dbUpdateEx when IsUniqueConstraintViolation(dbUpdateEx) => (
                StatusCodes.Status409Conflict,
                "A database conflict occurred: A record with the specified unique values already exists.",
                new List<string>()
            ),
            DbUpdateException => (
                StatusCodes.Status500InternalServerError,
                "A database error occurred while processing your request.",
                new List<string>()
            ),
            DbException => (
                StatusCodes.Status500InternalServerError,
                "A database error occurred while processing your request.",
                new List<string>()
            ),
            DatabaseException => (
                StatusCodes.Status500InternalServerError,
                "A database error occurred while processing your request.",
                new List<string>()
            ),

            // 8. Unexpected Exceptions (500 Internal Server Error)
            _ => (
                StatusCodes.Status500InternalServerError,
                _environment.IsDevelopment()
                    ? $"An unexpected error occurred: {exception.Message}"
                    : "An unexpected error occurred. Please try again later.",
                new List<string>()
            )
        };
    }

    private static bool IsForbiddenAccess(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        var lower = message.ToLowerInvariant();
        return lower.Contains("denied")
            || lower.Contains("forbidden")
            || lower.Contains("not authorized to view")
            || lower.Contains("not authorized to modify")
            || lower.Contains("not authorized to delete")
            || lower.Contains("not authorized to cancel")
            || lower.Contains("not authorized to access")
            || lower.Contains("not authorized to pay")
            || lower.Contains("not authorized to confirm")
            || lower.Contains("permission");
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var current = ex.InnerException;
        while (current != null)
        {
            var msg = current.Message.ToLowerInvariant();
            if (msg.Contains("23505") || msg.Contains("unique constraint") || msg.Contains("duplicate key"))
            {
                return true;
            }
            current = current.InnerException;
        }
        return false;
    }
}

/// <summary>
/// IApplicationBuilder extension methods for registering ExceptionMiddleware.
/// </summary>
public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionMiddleware>();
    }
}
