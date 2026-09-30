using Auction.API.Filters;
using Auction.BLL.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Auction.Tests;

public class ServiceResponseTests
{
    [Fact]
    public void Success_CreatesSuccessfulEnvelope()
    {
        var payload = new { id = 7 };

        var response = ServiceResponse.Success("Created.", payload);

        Assert.True(response.IsSuccess);
        Assert.Equal("Created.", response.Message);
        Assert.Same(payload, response.Payload);
    }

    [Fact]
    public async Task ResultFilter_WrapsSuccessfulObjectResult()
    {
        var payload = new { id = 7 };
        var context = CreateContext(new OkObjectResult(payload));

        await ExecuteFilterAsync(context);

        var result = Assert.IsType<OkObjectResult>(context.Result);
        var response = Assert.IsType<ServiceResponse>(result.Value);
        Assert.True(response.IsSuccess);
        Assert.Same(payload, response.Payload);
    }

    [Fact]
    public async Task ResultFilter_UsesErrorStringAsMessage()
    {
        var context = CreateContext(new BadRequestObjectResult("Invalid amount."));

        await ExecuteFilterAsync(context);

        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        var response = Assert.IsType<ServiceResponse>(result.Value);
        Assert.False(response.IsSuccess);
        Assert.Equal("Invalid amount.", response.Message);
        Assert.Null(response.Payload);
    }

    [Fact]
    public async Task ResultFilter_ChangesNoContentToEnvelopeResponse()
    {
        var context = CreateContext(new NoContentResult());

        await ExecuteFilterAsync(context);

        var result = Assert.IsType<OkObjectResult>(context.Result);
        var response = Assert.IsType<ServiceResponse>(result.Value);
        Assert.True(response.IsSuccess);
        Assert.Null(response.Payload);
    }

    private static ResultExecutingContext CreateContext(IActionResult result)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ResultExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            result,
            new object());
    }

    private static Task ExecuteFilterAsync(ResultExecutingContext context)
    {
        var filter = new ServiceResponseResultFilter();
        return filter.OnResultExecutionAsync(
            context,
            () => Task.FromResult(new ResultExecutedContext(
                context,
                new List<IFilterMetadata>(),
                context.Result,
                context.Controller)));
    }
}
