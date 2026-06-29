using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using EMS.Web.Middleware;

namespace EMS.Tests.Middleware;

public class RateLimitingMiddlewareTests
{
    private readonly Mock<ILogger<RateLimitingMiddleware>> _logger = new();

    private RateLimitingMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next, _logger.Object);

    private DefaultHttpContext CreateContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1." + Random.Shared.Next(1, 254));
        return context;
    }

    [Fact]
    public async Task AllowsGetRequests_WithoutRateLimit()
    {
        var called = false;
        var middleware = CreateMiddleware(_ => { called = true; return Task.CompletedTask; });

        var context = CreateContext("GET", "/Account/Login");
        await middleware.InvokeAsync(context);

        Assert.True(called);
        Assert.NotEqual(429, context.Response.StatusCode);
    }

    [Fact]
    public async Task AllowsNonRateLimitedPosts()
    {
        var called = false;
        var middleware = CreateMiddleware(_ => { called = true; return Task.CompletedTask; });

        var context = CreateContext("POST", "/Dashboard/Index");
        await middleware.InvokeAsync(context);

        Assert.True(called);
    }

    [Fact]
    public async Task AllowsFirstLoginPost()
    {
        var called = false;
        var middleware = CreateMiddleware(_ => { called = true; return Task.CompletedTask; });

        var context = CreateContext("POST", "/Account/Login");
        await middleware.InvokeAsync(context);

        Assert.True(called);
        Assert.NotEqual(429, context.Response.StatusCode);
    }

    [Fact]
    public async Task Blocks_After_ExcessiveLoginPosts()
    {
        var callCount = 0;
        var middleware = CreateMiddleware(_ => { callCount++; return Task.CompletedTask; });

        var ip = System.Net.IPAddress.Parse("10.0.0.99");

        for (int i = 0; i < 12; i++)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = "/Account/Login";
            context.Connection.RemoteIpAddress = ip;
            await middleware.InvokeAsync(context);

            if (i >= 10)
            {
                Assert.Equal(429, context.Response.StatusCode);
            }
        }

        Assert.Equal(10, callCount);
    }
}
