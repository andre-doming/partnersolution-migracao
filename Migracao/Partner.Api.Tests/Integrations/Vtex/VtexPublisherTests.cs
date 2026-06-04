using Partner.Api.Infrastructure.Integrations.Vtex;
using Xunit;

namespace Partner.Api.Tests.Integrations.Vtex;

public sealed class VtexPublisherTests
{
    [Fact]
    public void VtexSyncPublisher_HasCorrectRoutingKeys()
    {
        // Act & Assert
        Assert.Equal("partner.vtex.sync", VtexSyncPublisher.VtexSyncRoutingKey);
        Assert.Equal("partner.vtex.dlq", VtexSyncPublisher.VtexDlqRoutingKey);
    }

    [Fact]
    public void VtexSyncMessage_HasAllRequiredProperties()
    {
        // Arrange
        var jobPublicId = Guid.NewGuid();
        var correlationId = "test-correlation-id";

        // Act
        var message = new VtexSyncMessage
        {
            MessageId = Guid.NewGuid().ToString("N"),
            CorrelationId = correlationId,
            JobId = 123,
            JobPublicId = jobPublicId,
            Feature = "clients-csv",
            CompanyId = 456,
            UserId = 789,
            TotalRows = 100,
            SuccessRows = 95,
            ErrorRows = 5,
            DurationMs = 5000,
            CompletedAtUtc = DateTime.UtcNow,
            JobDataUri = "/api/import/jobs/test"
        };

        // Assert
        Assert.NotEmpty(message.MessageId);
        Assert.Equal(correlationId, message.CorrelationId);
        Assert.Equal(123, message.JobId);
        Assert.Equal(jobPublicId, message.JobPublicId);
        Assert.Equal("clients-csv", message.Feature);
        Assert.Equal(456, message.CompanyId);
        Assert.Equal(789, message.UserId);
        Assert.Equal(100, message.TotalRows);
        Assert.Equal(95, message.SuccessRows);
        Assert.Equal(5, message.ErrorRows);
        Assert.Equal(5000, message.DurationMs);
    }

    [Fact]
    public void VtexSyncMessage_CompletedAtUtc_DefaultsToUtcNow()
    {
        // Arrange & Act
        var before = DateTime.UtcNow;
        var message = new VtexSyncMessage();
        var after = DateTime.UtcNow;

        // Assert
        Assert.True(message.CompletedAtUtc >= before && message.CompletedAtUtc <= after);
    }

    [Fact]
    public void VtexSyncMessage_MessageId_GeneratedByDefault()
    {
        // Act
        var message = new VtexSyncMessage();

        // Assert
        Assert.NotEmpty(message.MessageId);
        Assert.Equal(32, message.MessageId.Length); // GUID sem hífens tem 32 caracteres
    }
}
