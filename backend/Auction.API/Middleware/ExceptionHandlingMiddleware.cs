using Auction.BLL.Services;
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
            var statusCode = GetStatusCode(exception);

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

            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteResponseAsync(context, exception, statusCode);
        }
    }

    private async Task WriteResponseAsync(
        HttpContext context,
        Exception exception,
        int statusCode)
    {
        var message = statusCode < StatusCodes.Status500InternalServerError ||
                      _environment.IsDevelopment()
            ? exception.Message
            : "An unexpected server error occurred.";
        var response = ServiceResponse.Error(message, new
        {
            traceId = context.TraceIdentifier
        });

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(response, context.RequestAborted);
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            ValidationException or ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status409Conflict,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }
}
