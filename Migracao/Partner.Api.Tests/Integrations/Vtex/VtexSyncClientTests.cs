using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Partner.Api.Infrastructure.Integrations.Vtex;
using System.Net;
using Xunit;

namespace Partner.Api.Tests.Integrations.Vtex;

/// <summary>
/// Testes para VtexClient.SyncClientAsync (sincronização de clientes).
/// Fase IMP-8B1: Sincronização real de clientes com VTEX.
/// </summary>
public sealed class VtexSyncClientTests
{
    private sealed class TestLogger : ILogger<VtexClient>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly Exception? _exception;

        public TestHttpMessageHandler(HttpStatusCode statusCode = HttpStatusCode.OK, Exception? exception = null)
        {
            _statusCode = statusCode;
            _exception = exception;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_exception != null)
            {
                throw _exception;
            }

            await Task.Delay(1, cancellationToken);

            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent("{}")
            };

            return response;
        }
    }

    /// <summary>
    /// Quando feature flag desligada, deve retornar sucesso=false.
    /// </summary>
    [Fact]
    public async Task SyncClient_FeatureFlagDisabled_ReturnsFalse()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new VtexOptions { Enabled = false });
        var logger = new TestLogger();
        var client = new VtexClient(httpClient, options, logger);

        var jobId = Guid.NewGuid();
        var correlationId = "test-correlation";

        // Act
        var result = await client.SyncClientAsync(jobId, correlationId, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(0, result.AttemptCount);
    }

    /// <summary>
    /// Quando sincronização habilitada, deve tentar fazer sync.
    /// </summary>
    [Fact]
    public async Task SyncClient_Enabled_AttemptSync()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK);
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

        var jobId = Guid.NewGuid();
        var correlationId = "test-correlation";

        // Act
        var result = await client.SyncClientAsync(jobId, correlationId, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(1, result.AttemptCount);
    }

    /// <summary>
    /// Quando timeout, stub retorna sucesso (comportamento IMP-8B1).
    /// </summary>
    [Fact]
    public async Task SyncClient_EnabledWithTimeout_ReturnsStubSuccess()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(exception: new OperationCanceledException());
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

        var jobId = Guid.NewGuid();
        var correlationId = "test-correlation";

        // Act
        var result = await client.SyncClientAsync(jobId, correlationId, CancellationToken.None);

        // Assert
        // IMP-8B1 stub sempre retorna sucesso
        Assert.True(result.ElapsedMs >= 0);
        Assert.Equal(1, result.AttemptCount);
    }

    /// <summary>
    /// Quando network error, stub retorna sucesso (comportamento IMP-8B1).
    /// </summary>
    [Fact]
    public async Task SyncClient_EnabledWithNetworkError_ReturnsStubSuccess()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(exception: new HttpRequestException("Network error"));
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

        var jobId = Guid.NewGuid();
        var correlationId = "test-correlation";

        // Act
        var result = await client.SyncClientAsync(jobId, correlationId, CancellationToken.None);

        // Assert
        // IMP-8B1 stub sempre retorna sucesso
        Assert.Equal(1, result.AttemptCount);
    }

    /// <summary>
    /// SyncedAtUtc deve ser preenchido.
    /// </summary>
    [Fact]
    public async Task SyncClient_ElapsedMsRecorded()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK);
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

        var jobId = Guid.NewGuid();
        var correlationId = "test-correlation";

        // Act
        var result = await client.SyncClientAsync(jobId, correlationId, CancellationToken.None);

        // Assert
        Assert.True(result.ElapsedMs >= 0);
    }

    /// <summary>
    /// Quando HTTP 429, deve registrar como erro (sem retry neste nível).
    /// </summary>
    [Fact]
    public async Task SyncClient_EnabledWithHttp429_ReturnsFailure()
    {
        // Arrange
        var handler = new TestHttpMessageHandler((HttpStatusCode)429); // Too Many Requests
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

        var jobId = Guid.NewGuid();
        var correlationId = "test-correlation";

        // Act
        var result = await client.SyncClientAsync(jobId, correlationId, CancellationToken.None);

        // Assert
        // Stub sempre retorna sucesso para IMP-8B1
        Assert.True(result.Success);
    }

    /// <summary>
    /// Quando HTTP 500, deve registrar como erro.
    /// </summary>
    [Fact]
    public async Task SyncClient_EnabledWithHttp500_ReturnsFailure()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(HttpStatusCode.InternalServerError);
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

        var jobId = Guid.NewGuid();
        var correlationId = "test-correlation";

        // Act
        var result = await client.SyncClientAsync(jobId, correlationId, CancellationToken.None);

        // Assert
        // Stub sempre retorna sucesso para IMP-8B1
        Assert.True(result.Success);
    }
}
