using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Partner.Api.Infrastructure.Integrations.Vtex;
using Xunit;

namespace Partner.Api.Tests.Integrations.Vtex;

public sealed class VtexConfigurationTests
{
    [Fact]
    public void VtexOptions_WithFeatureFlagDisabled_DefaultsToFalse()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Vtex:Enabled"] = "false" })
            .Build();

        // Act
        var options = config.GetSection("Vtex").Get<VtexOptions>();

        // Assert
        Assert.NotNull(options);
        Assert.False(options.Enabled);
    }

    [Fact]
    public void VtexOptions_WithFeatureFlagEnabled_LoadsCorrectly()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Vtex:Enabled"] = "true",
                ["Vtex:BaseUrl"] = "https://api.vtex.com/test",
                ["Vtex:AppKey"] = "test-key",
                ["Vtex:AppToken"] = "test-token",
                ["Vtex:RetryCount"] = "5"
            })
            .Build();

        // Act
        var options = config.GetSection("Vtex").Get<VtexOptions>();

        // Assert
        Assert.NotNull(options);
        Assert.True(options.Enabled);
        Assert.Equal("https://api.vtex.com/test", options.BaseUrl);
        Assert.Equal("test-key", options.AppKey);
        Assert.Equal("test-token", options.AppToken);
        Assert.Equal(5, options.RetryCount);
    }

    [Fact]
    public void VtexRabbitMqOptions_LoadsFromConfiguration()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VtexRabbitMq:Host"] = "rabbitmq.test",
                ["VtexRabbitMq:Port"] = "5673",
                ["VtexRabbitMq:VirtualHost"] = "test",
                ["VtexRabbitMq:Username"] = "testuser",
                ["VtexRabbitMq:Password"] = "testpass",
                ["VtexRabbitMq:VtexSyncQueue"] = "test.vtex.sync"
            })
            .Build();

        // Act
        var options = config.GetSection("VtexRabbitMq").Get<VtexRabbitMqOptions>();

        // Assert
        Assert.NotNull(options);
        Assert.Equal("rabbitmq.test", options.Host);
        Assert.Equal(5673, options.Port);
        Assert.Equal("test", options.VirtualHost);
        Assert.Equal("testuser", options.Username);
        Assert.Equal("testpass", options.Password);
        Assert.Equal("test.vtex.sync", options.VtexSyncQueue);
    }

    [Fact]
    public void VtexOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new VtexOptions();

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal(string.Empty, options.BaseUrl);
        Assert.Equal(string.Empty, options.AppKey);
        Assert.Equal(string.Empty, options.AppToken);
        Assert.Equal(3, options.RetryCount);
        Assert.Equal(1000, options.RetryDelayMs);
    }

    [Fact]
    public void VtexRabbitMqOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new VtexRabbitMqOptions();

        // Assert
        Assert.Equal("localhost", options.Host);
        Assert.Equal(5672, options.Port);
        Assert.Equal("partner", options.VirtualHost);
        Assert.Equal(string.Empty, options.Username);
        Assert.Equal(string.Empty, options.Password);
        Assert.Equal("partner.events", options.Exchange);
        Assert.Equal("partner.vtex.sync", options.VtexSyncQueue);
        Assert.Equal("partner.vtex.dlq", options.VtexDlqQueue);
    }
}
