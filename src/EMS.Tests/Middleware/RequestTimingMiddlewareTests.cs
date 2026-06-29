using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using EMS.Web.Middleware;

namespace EMS.Tests.Middleware;

public class RequestTimingMiddlewareTests
{
    private readonly Mock<ILogger<RequestTimingMiddleware>> _logger = new();

    [Fact]
    public async Task AddsRequestIdHeader()
    {
        var middleware = new RequestTimingMiddleware(_ => Task.CompletedTask, _logger.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/test";

        await middleware.InvokeAsync(context);

        Assert.True(context.Response.Headers.ContainsKey("X-Request-Id"));
        Assert.Equal(8, context.Response.Headers["X-Request-Id"].ToString().Length);
    }

    [Fact]
    public async Task CallsNextMiddleware()
    {
        var called = false;
        var middleware = new RequestTimingMiddleware(_ => { called = true; return Task.CompletedTask; }, _logger.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/";

        await middleware.InvokeAsync(context);

        Assert.True(called);
    }

    [Fact]
    public async Task StillAddsHeader_WhenNextThrows()
    {
        var middleware = new RequestTimingMiddleware(_ => throw new Exception("Boom"), _logger.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/error";

        await Assert.ThrowsAsync<Exception>(() => middleware.InvokeAsync(context));

        Assert.True(context.Response.Headers.ContainsKey("X-Request-Id"));
    }
}
