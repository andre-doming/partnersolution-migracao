using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Partner.Api.Infrastructure.Integrations.Vtex;
using System.Net;
using Xunit;

namespace Partner.Api.Tests.Integrations.Vtex;

/// <summary>
/// Testes para VtexClient - comportamento geral e sincronização (stub).
/// Testes de health check estão em VtexHealthCheckTests.cs
/// </summary>
public sealed class VtexClientTests
{
    private sealed class TestLogger : ILogger<VtexClient>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        }
    }

    /// <summary>
    /// SyncClientAsync (stub) deve lançar NotImplementedException.
    /// </summary>
    [Fact]
    public async Task SyncClientAsync_IsStubInImp8A_ThrowsNotImplementedException()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new VtexOptions { Enabled = true });
        var logger = new TestLogger();
        var client = new VtexClient(httpClient, options, logger);

        var jobPublicId = Guid.NewGuid();
        var correlationId = "test-correlation";

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotImplementedException>(
            () => client.SyncClientAsync(jobPublicId, correlationId, CancellationToken.None)
        );

        Assert.NotNull(ex);
        Assert.Contains("IMP-8B", ex.Message);
    }

    /// <summary>
    /// SyncClientAsync (stub) deve lançar NotImplementedException mesmo quando desabilitado.
    /// </summary>
    [Fact]
    public async Task SyncClientAsync_WithFeatureFlagDisabled_StillThrowsNotImplementedException()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new VtexOptions { Enabled = false });
        var logger = new TestLogger();
        var client = new VtexClient(httpClient, options, logger);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotImplementedException>(
            () => client.SyncClientAsync(Guid.NewGuid(), "test-correlation", CancellationToken.None)
        );

        Assert.NotNull(ex);
        Assert.Contains("fase futura", ex.Message);
    }
}
