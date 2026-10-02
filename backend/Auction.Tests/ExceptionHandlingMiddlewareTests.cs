using Auction.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Auction.Tests;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenResponseHasStarted_RethrowsOriginalException()
    {
        var context = new DefaultHttpContext();
        var responseFeature = new Mock<IHttpResponseFeature>();
        responseFeature.SetupGet(feature => feature.HasStarted).Returns(true);
        context.Features.Set(responseFeature.Object);
        var expected = new InvalidOperationException("Stream failed.");
        var middleware = new ExceptionHandlingMiddleware(
            _ =>
            {
                return Task.FromException(expected);
            },
            NullLogger<ExceptionHandlingMiddleware>.Instance,
            Mock.Of<IHostEnvironment>());

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context));

        Assert.True(context.Response.HasStarted);
        Assert.Same(expected, actual);
    }
}
