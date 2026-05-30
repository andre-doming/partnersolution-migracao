using Serilog.Context;

namespace Partner.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("correlationId", correlationId))
        {
            await _next(context);
        }
    }

    public static string GetCorrelationId(HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var value) && value is string text && !string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return context.TraceIdentifier;
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var provided = context.Request.Headers[HeaderName].ToString().Trim();
        if (!string.IsNullOrWhiteSpace(provided))
        {
            return provided;
        }

        return Guid.NewGuid().ToString("N");
    }
}

