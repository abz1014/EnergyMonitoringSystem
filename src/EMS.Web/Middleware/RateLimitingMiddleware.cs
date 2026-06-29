using System.Collections.Concurrent;

namespace EMS.Web.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _requests = new();
    private const int MaxRequestsPerWindow = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private static readonly HashSet<string> RateLimitedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/Account/Login",
        "/Account/Register",
        "/api/auth/login",
        "/api/auth/register"
    };

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;

        if (method == "POST" && RateLimitedPaths.Contains(path))
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"{clientIp}:{path}";
            var now = DateTime.UtcNow;

            var entry = _requests.GetOrAdd(key, _ => (0, now));

            if (now - entry.WindowStart > Window)
            {
                entry = (1, now);
                _requests[key] = entry;
            }
            else
            {
                entry = (entry.Count + 1, entry.WindowStart);
                _requests[key] = entry;
            }

            if (entry.Count > MaxRequestsPerWindow)
            {
                _logger.LogWarning("Rate limit exceeded for {ClientIp} on {Path} ({Count} requests in window)",
                    clientIp, path, entry.Count);

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = "60";
                await context.Response.WriteAsync("Too many requests. Please wait a minute before trying again.");
                return;
            }
        }

        await _next(context);
    }
}
