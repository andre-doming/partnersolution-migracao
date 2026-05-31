using System.Text;
using Microsoft.AspNetCore.Http;
using Partner.Api.Infrastructure.RateLimiting;
using Partner.Api.Middleware;

namespace Partner.Api.Tests.RateLimiting;

public sealed class RateLimitingBodyCaptureMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldStoreBodyInContextItems()
    {
        var context = new DefaultHttpContext();
        var payload = "{\"login\":\"user\"}";
        context.Request.Method = HttpMethods.Post;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.ContentLength = context.Request.Body.Length;

        var middleware = new RateLimitingBodyCaptureMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.True(context.Items.ContainsKey(RateLimitingMetadataKeys.RequestBody));
        Assert.Equal(payload, context.Items[RateLimitingMetadataKeys.RequestBody]);
    }
}