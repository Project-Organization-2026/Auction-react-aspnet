using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Auction.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
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
        catch (Exception exception)
        {
            var (statusCode, title) = GetErrorDetails(exception);

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "An unhandled exception occurred.");
            }
            else
            {
                _logger.LogWarning(
                    exception,
                    "Request failed with status code {StatusCode}.",
                    statusCode);
            }

            await WriteResponseAsync(context, exception, statusCode, title);
        }
    }

    private async Task WriteResponseAsync(
        HttpContext context,
        Exception exception,
        int statusCode,
        string title)
    {
        var response = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode < StatusCodes.Status500InternalServerError ||
                     _environment.IsDevelopment()
                ? exception.Message
                : "An unexpected server error occurred.",
            Instance = context.Request.Path
        };
        response.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(response, context.RequestAborted);
    }

    private static (int StatusCode, string Title) GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found"),
            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Access forbidden"),
            ValidationException or ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Invalid request"),
            InvalidOperationException => (
                StatusCodes.Status409Conflict,
                "Operation conflict"),
            _ => ((int)HttpStatusCode.InternalServerError, "Server error")
        };
    }
}
