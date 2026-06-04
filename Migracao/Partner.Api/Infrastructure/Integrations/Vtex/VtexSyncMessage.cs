namespace Partner.Api.Infrastructure.Integrations.Vtex;

using System.Text.Json.Serialization;

/// <summary>
/// Mensagem de sincronização com VTEX.
/// Publicada na fila vtex.sync quando uma importação é concluída com sucesso.
/// </summary>
public sealed record VtexSyncMessage
{
    [JsonPropertyName("messageId")]
    public string MessageId { get; init; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; init; } = string.Empty;

    [JsonPropertyName("jobId")]
    public int JobId { get; init; }

    [JsonPropertyName("jobPublicId")]
    public Guid JobPublicId { get; init; }

    [JsonPropertyName("feature")]
    public string Feature { get; init; } = string.Empty;

    [JsonPropertyName("companyId")]
    public int CompanyId { get; init; }

    [JsonPropertyName("userId")]
    public int UserId { get; init; }

    [JsonPropertyName("totalRows")]
    public int TotalRows { get; init; }

    [JsonPropertyName("successRows")]
    public int SuccessRows { get; init; }

    [JsonPropertyName("errorRows")]
    public int ErrorRows { get; init; }

    [JsonPropertyName("durationMs")]
    public int DurationMs { get; init; }

    [JsonPropertyName("completedAtUtc")]
    public DateTime CompletedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Referência para recuperar dados completos da importação
    /// </summary>
    [JsonPropertyName("jobDataUri")]
    public string JobDataUri { get; init; } = string.Empty;
}
