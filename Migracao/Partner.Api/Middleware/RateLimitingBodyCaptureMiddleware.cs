using Partner.Api.Infrastructure.RateLimiting;

namespace Partner.Api.Middleware;

public sealed class RateLimitingBodyCaptureMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimitingBodyCaptureMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && context.Request.ContentLength is > 0)
        {
            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (!string.IsNullOrWhiteSpace(body))
            {
                context.Items[RateLimitingMetadataKeys.RequestBody] = body;
            }
        }

        await _next(context);
    }
}