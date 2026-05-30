using Partner.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace Partner.Api.Tests.Observability;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task Should_Reuse_RequestHeader_CorrelationId()
    {
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "corr-fixed-001";

        await middleware.Invoke(context);

        Assert.Equal("corr-fixed-001", context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
        Assert.Equal("corr-fixed-001", CorrelationIdMiddleware.GetCorrelationId(context));
    }

    [Fact]
    public async Task Should_Generate_CorrelationId_WhenHeaderMissing()
    {
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        await middleware.Invoke(context);

        var responseHeader = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(responseHeader));
        Assert.Equal(responseHeader, CorrelationIdMiddleware.GetCorrelationId(context));
    }
}

