using System.Diagnostics;

namespace EMS.Web.Middleware;

public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];

        context.Response.Headers["X-Request-Id"] = requestId;

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var elapsed = sw.ElapsedMilliseconds;
            var path = context.Request.Path;
            var method = context.Request.Method;
            var status = context.Response.StatusCode;

            if (elapsed > 500)
            {
                _logger.LogWarning("[{RequestId}] {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode} (SLOW)",
                    requestId, method, path, elapsed, status);
            }
            else
            {
                _logger.LogInformation("[{RequestId}] {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
                    requestId, method, path, elapsed, status);
            }
        }
    }
}
