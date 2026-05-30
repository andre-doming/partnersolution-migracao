using System.Diagnostics;

namespace Partner.Api.Middleware;

public sealed class RequestMetricsMiddleware
{
    private const string ElapsedMsItemKey = "RequestElapsedMs";
    private readonly RequestDelegate _next;

    public RequestMetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            context.Items[ElapsedMsItemKey] = stopwatch.Elapsed.TotalMilliseconds;
        }
    }

    public static double GetElapsedMs(HttpContext context)
    {
        if (context.Items.TryGetValue(ElapsedMsItemKey, out var value) && value is double elapsedMs)
        {
            return elapsedMs;
        }

        return 0d;
    }
}

