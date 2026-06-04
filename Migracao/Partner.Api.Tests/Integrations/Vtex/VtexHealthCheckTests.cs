using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Partner.Api.Infrastructure.Integrations.Vtex;
using System.Net;
using Xunit;

namespace Partner.Api.Tests.Integrations.Vtex;

/// <summary>
/// Testes para VtexClient.CheckConnectionAsync (Health Check).
/// </summary>
public sealed class VtexHealthCheckTests
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

            await Task.Delay(1, cancellationToken); // Simular latência

            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent("{}")
            };

            return response;
        }
    }

    /// <summary>
    /// Quando Vtex:Enabled=false, deve retornar Disabled sem fazer chamada HTTP.
    /// </summary>
    [Fact]
    public async Task HealthCheck_FeatureFlagDisabled_DoesNotCallVtex()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new VtexOptions { Enabled = false });
        var logger = new TestLogger();
        var client = new VtexClient(httpClient, options, logger);

        // Act
        var result = await client.CheckConnectionAsync("test-correlation", CancellationToken.None);

        // Assert
        Assert.Equal(VtexHealthStatus.Disabled, result.Status);
        Assert.Equal(0, result.ResponseTimeMs);
        Assert.Null(result.StatusCode);
        Assert.NotNull(result.Message);
    }

    /// <summary>
    /// Quando Vtex:Enabled=true e HTTP 200, deve retornar Connected.
    /// </summary>
    [Fact]
    public async Task HealthCheck_EnabledWithHttp200_ReturnsConnected()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.vtex.com/mystore") };
        var options = Options.Create(new VtexOptions
        {
            Enabled = true,
            BaseUrl = "https://api.vtex.com/mystore",
            AppKey = "test-key",
            AppToken = "test-token"
        });
        var logger = new TestLogger();
        var client = new VtexClient(httpClient, options, logger);

        // Act
        var result = await client.CheckConnectionAsync("test-correlation", CancellationToken.None);

        // Assert
        Assert.Equal(VtexHealthStatus.Connected, result.Status);
        Assert.True(result.ResponseTimeMs > 0);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Message);
    }

    /// <summary>
    /// Quando Vtex:Enabled=true e HTTP 401, deve retornar Unauthorized SEM retry.
    /// </summary>
    [Fact]
    public async Task HealthCheck_EnabledWithHttp401_ReturnsUnauthorized()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(HttpStatusCode.Unauthorized);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.vtex.com/mystore") };
        var options = Options.Create(new VtexOptions
        {
            Enabled = true,
            BaseUrl = "https://api.vtex.com/mystore",
            AppKey = "test-key",
            AppToken = "test-token"
        });
        var logger = new TestLogger();
        var client = new VtexClient(httpClient, options, logger);

        // Act
        var result = await client.CheckConnectionAsync("test-correlation", CancellationToken.None);

        // Assert
        Assert.Equal(VtexHealthStatus.Unauthorized, result.Status);
        Assert.Equal(401, result.StatusCode);
    }

    /// <summary>
    /// Quando Vtex:Enabled=true e HTTP 403, deve retornar Forbidden SEM retry.
    /// </summary>
    [Fact]
    public async Task HealthCheck_EnabledWithHttp403_ReturnsForbidden()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(HttpStatusCode.Forbidden);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.vtex.com/mystore") };
        var options = Options.Create(new VtexOptions
        {
            Enabled = true,
            BaseUrl = "https://api.vtex.com/mystore",
            AppKey = "test-key",
            AppToken = "test-token"
        });
        var logger = new TestLogger();
        var client = new VtexClient(httpClient, options, logger);

        // Act
        var result = await client.CheckConnectionAsync("test-correlation", CancellationToken.None);

        // Assert
        Assert.Equal(VtexHealthStatus.Forbidden, result.Status);
        Assert.Equal(403, result.StatusCode);
    }

    /// <summary>
    /// Quando Vtex:Enabled=true e HTTP 404, deve retornar NotFound SEM retry.
    /// </summary>
    [Fact]
    public async Task HealthCheck_EnabledWithHttp404_ReturnsNotFound()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(HttpStatusCode.NotFound);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.vtex.com/mystore") };
        var options = Options.Create(new VtexOptions
        {
            Enabled = true,
            BaseUrl = "https://api.vtex.com/mystore",
            AppKey = "test-key",
            AppToken = "test-token"
        });
        var logger = new TestLogger();
        var client = new VtexClient(httpClient, options, logger);

        // Act
        var result = await client.CheckConnectionAsync("test-correlation", CancellationToken.None);

        // Assert
        Assert.Equal(VtexHealthStatus.NotFound, result.Status);
        Assert.Equal(404, result.StatusCode);
    }

    /// <summary>
    /// Quando timeout, deve retornar Timeout (retry é responsabilidade do HttpClient resilience handler).
    /// </summary>
    [Fact]
    public async Task HealthCheck_EnabledWithTimeout_ReturnsTimeout()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(exception: new OperationCanceledException());
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.vtex.com/mystore") };
        var options = Options.Create(new VtexOptions
        {
            Enabled = true,
            BaseUrl = "https://api.vtex.com/mystore",
            AppKey = "test-key",
            AppToken = "test-token"
        });
        var logger = new TestLogger();
        var client = new VtexClient(httpClient, options, logger);

        // Act
        var result = await client.CheckConnectionAsync("test-correlation", CancellationToken.None);

        // Assert
        Assert.Equal(VtexHealthStatus.Timeout, result.Status);
        Assert.Null(result.StatusCode);
    }

    /// <summary>
    /// Quando network error, deve retornar Disconnected.
    /// </summary>
    [Fact]
    public async Task HealthCheck_EnabledWithNetworkError_ReturnsDisconnected()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(exception: new HttpRequestException("Network error"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.vtex.com/mystore") };
        var options = Options.Create(new VtexOptions
        {
            Enabled = true,
            BaseUrl = "https://api.vtex.com/mystore",
            AppKey = "test-key",
            AppToken = "test-token"
        });
        var logger = new TestLogger();
        var client = new VtexClient(httpClient, options, logger);

        // Act
        var result = await client.CheckConnectionAsync("test-correlation", CancellationToken.None);

        // Assert
        Assert.Equal(VtexHealthStatus.Disconnected, result.Status);
        Assert.Null(result.StatusCode);
    }

    /// <summary>
    /// Quando HTTP 500, deve retornar Disconnected (retry é responsabilidade do resilience handler).
    /// </summary>
    [Fact]
    public async Task HealthCheck_EnabledWithHttp500_ReturnsDisconnected()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.vtex.com/mystore") };
        var options = Options.Create(new VtexOptions
        {
            Enabled = true,
            BaseUrl = "https://api.vtex.com/mystore",
            AppKey = "test-key",
            AppToken = "test-token"
        });
        var logger = new TestLogger();
        var client = new VtexClient(httpClient, options, logger);

        // Act
        var result = await client.CheckConnectionAsync("test-correlation", CancellationToken.None);

        // Assert
        Assert.Equal(VtexHealthStatus.Disconnected, result.Status);
        Assert.Equal(500, result.StatusCode);
    }

    /// <summary>
    /// Quando configuração inválida (BaseUrl vazio), deve retornar Disconnected.
    /// </summary>
    [Fact]
    public async Task HealthCheck_InvalidConfiguration_ReturnsDisconnected()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new VtexOptions
        {
            Enabled = true,
            BaseUrl = string.Empty,
            AppKey = string.Empty,
            AppToken = string.Empty
        });
        var logger = new TestLogger();
        var client = new VtexClient(httpClient, options, logger);

        // Act
        var result = await client.CheckConnectionAsync("test-correlation", CancellationToken.None);

        // Assert
        Assert.Equal(VtexHealthStatus.Disconnected, result.Status);
        Assert.Null(result.StatusCode);
        Assert.NotNull(result.Message);
    }

    /// <summary>
    /// ResponseTimeMs deve ser preenchido corretamente.
    /// </summary>
    [Fact]
    public async Task HealthCheck_ResponseTime_IsRecorded()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.vtex.com/mystore") };
        var options = Options.Create(new VtexOptions
        {
            Enabled = true,
            BaseUrl = "https://api.vtex.com/mystore",
            AppKey = "test-key",
            AppToken = "test-token"
        });
        var logger = new TestLogger();
        var client = new VtexClient(httpClient, options, logger);

        // Act
        var result = await client.CheckConnectionAsync("test-correlation", CancellationToken.None);

        // Assert
        Assert.True(result.ResponseTimeMs >= 0, "ResponseTimeMs should be recorded");
    }
}
