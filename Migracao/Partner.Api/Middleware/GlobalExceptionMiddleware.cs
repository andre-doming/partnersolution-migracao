using Partner.Api.Shared.Contracts;
using System.Text.Json;

namespace Partner.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                TraceId = context.TraceIdentifier,
                CorrelationId = CorrelationIdMiddleware.GetCorrelationId(context),
                Code = "unexpected_error",
                Message = "Ocorreu um erro inesperado. Tente novamente."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
