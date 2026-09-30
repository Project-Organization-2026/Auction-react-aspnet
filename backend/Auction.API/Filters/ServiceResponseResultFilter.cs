using Auction.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Auction.API.Filters;

public sealed class ServiceResponseResultFilter : IAsyncAlwaysRunResultFilter
{
    public async Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        context.Result = WrapResult(context.Result);
        await next();
    }

    internal static IActionResult WrapResult(IActionResult result)
    {
        if (result is ObjectResult objectResult)
        {
            if (objectResult.Value is ServiceResponse)
            {
                return objectResult;
            }

            var statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;
            objectResult.Value = CreateResponse(
                statusCode,
                objectResult.Value);
            return objectResult;
        }

        if (result is ForbidResult)
        {
            return CreateObjectResult(
                StatusCodes.Status403Forbidden,
                ServiceResponse.Error("Access is forbidden."));
        }

        if (result is ChallengeResult)
        {
            return CreateObjectResult(
                StatusCodes.Status401Unauthorized,
                ServiceResponse.Error("Authentication is required."));
        }

        if (result is NoContentResult)
        {
            return new OkObjectResult(ServiceResponse.Success());
        }

        if (result is StatusCodeResult statusCodeResult)
        {
            var statusCode = statusCodeResult.StatusCode ==
                             StatusCodes.Status204NoContent
                ? StatusCodes.Status200OK
                : statusCodeResult.StatusCode;
            return CreateObjectResult(
                statusCode,
                CreateResponse(statusCode, null));
        }

        if (result is EmptyResult)
        {
            return new OkObjectResult(ServiceResponse.Success());
        }

        return result;
    }

    private static ServiceResponse CreateResponse(int statusCode, object? value)
    {
        if (statusCode is >= 200 and < 300)
        {
            var message = statusCode == StatusCodes.Status201Created
                ? "Resource created successfully."
                : "Request completed successfully.";
            return ServiceResponse.Success(message, value);
        }

        if (value is string message)
        {
            return ServiceResponse.Error(message);
        }

        return ServiceResponse.Error(GetDefaultErrorMessage(statusCode), value);
    }

    private static ObjectResult CreateObjectResult(
        int statusCode,
        ServiceResponse response)
    {
        return new ObjectResult(response) { StatusCode = statusCode };
    }

    internal static string GetDefaultErrorMessage(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Request validation failed.",
            StatusCodes.Status401Unauthorized => "Authentication is required.",
            StatusCodes.Status403Forbidden => "Access is forbidden.",
            StatusCodes.Status404NotFound => "Resource was not found.",
            StatusCodes.Status405MethodNotAllowed => "HTTP method is not allowed.",
            StatusCodes.Status409Conflict => "The request conflicts with the current state.",
            _ when statusCode >= 500 => "An unexpected server error occurred.",
            _ => "Request failed."
        };
    }
}
