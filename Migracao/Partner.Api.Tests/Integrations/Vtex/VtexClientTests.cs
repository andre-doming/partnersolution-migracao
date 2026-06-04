using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Partner.Api.Infrastructure.Integrations.Vtex;
using Xunit;

namespace Partner.Api.Tests.Integrations.Vtex;

public sealed class VtexClientTests
{
    private sealed class TestLogger : ILogger<VtexClient>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    [Fact]
    public async Task SyncClientAsync_ThrowsNotImplementedException()
    {
        // Arrange
        var options = Options.Create(new VtexOptions { Enabled = false });
        var logger = new TestLogger();
        var client = new VtexClient(options, logger);

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(
            () => client.SyncClientAsync(Guid.NewGuid(), "test-correlation", CancellationToken.None)
        );
    }

    [Fact]
    public async Task SyncClientAsync_WithFeatureFlagEnabled_ThrowsNotImplementedException()
    {
        // Arrange
        var options = Options.Create(new VtexOptions { Enabled = true });
        var logger = new TestLogger();
        var client = new VtexClient(options, logger);

        var jobPublicId = Guid.NewGuid();
        var correlationId = "test-correlation";

        // Act
        var ex = await Assert.ThrowsAsync<NotImplementedException>(
            () => client.SyncClientAsync(jobPublicId, correlationId, CancellationToken.None)
        );

        // Assert
        Assert.NotNull(ex);
        Assert.Contains("VTEX sync será implementado", ex.Message);
    }

    [Fact]
    public async Task SyncClientAsync_WithValidParameters_ReturnsNotImplementedException()
    {
        // Arrange
        var options = Options.Create(new VtexOptions { Enabled = true });
        var logger = new TestLogger();
        var client = new VtexClient(options, logger);

        var jobPublicId = Guid.NewGuid();
        var correlationId = "test-correlation-id";

        // Act
        var ex = await Assert.ThrowsAsync<NotImplementedException>(
            () => client.SyncClientAsync(jobPublicId, correlationId, CancellationToken.None)
        );

        // Assert
        Assert.NotNull(ex);
        Assert.Contains("fase futura", ex.Message);
    }
}
