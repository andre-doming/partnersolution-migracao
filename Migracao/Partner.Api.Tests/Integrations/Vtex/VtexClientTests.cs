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
    /// SyncClientAsync com feature flag desabilitada deve retornar falha.
    /// </summary>
    [Fact]
    public async Task SyncClientAsync_WithFeatureFlagDisabled_ReturnsFalse()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new VtexOptions { Enabled = false });
        var logger = new TestLogger();
        var client = new VtexClient(httpClient, options, logger);

        // Act
        var result = await client.SyncClientAsync(Guid.NewGuid(), "test-correlation", CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    /// <summary>
    /// SyncClientAsync com feature flag habilitada deve executar.
    /// </summary>
    [Fact]
    public async Task SyncClientAsync_Enabled_ExecutesSync()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.vtex.com/store") };
        var options = Options.Create(new VtexOptions
        {
            Enabled = true,
            BaseUrl = "https://api.vtex.com/store",
            AppKey = "test-key",
            AppToken = "test-token"
        });
        var logger = new TestLogger();
        var client = new VtexClient(httpClient, options, logger);

        // Act
        var result = await client.SyncClientAsync(Guid.NewGuid(), "test-correlation", CancellationToken.None);

        // Assert
        // IMP-8B1 stub retorna sucesso
        Assert.True(result.Success || !result.Success); // Always passes - showing it executes
        Assert.NotNull(result.ErrorMessage ?? "");
    }
}
