namespace Partner.Api.Infrastructure.Integrations.Vtex;

using System.Text.Json.Serialization;

/// <summary>
/// DTO para sincronização de cliente com VTEX Master Data API.
/// Mapeado a partir do modelo Client da aplicação.
/// Fase IMP-8B1: Sincronização real de clientes.
/// </summary>
public sealed record VtexClientSyncRequest
{
    /// <summary>
    /// Nome do cliente (first name)
    /// </summary>
    [JsonPropertyName("firstName")]
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// Sobrenome do cliente (last name)
    /// </summary>
    [JsonPropertyName("lastName")]
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Email do cliente
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Documento do cliente (CPF/CNPJ)
    /// Mapeado do campo Document
    /// </summary>
    [JsonPropertyName("document")]
    public string Document { get; init; } = string.Empty;

    /// <summary>
    /// ID da empresa (CompanyId)
    /// Enviado como string para compatibilidade VTEX
    /// </summary>
    [JsonPropertyName("companyId")]
    public string CompanyId { get; init; } = string.Empty;

    /// <summary>
    /// Status ativo/inativo (IsActive)
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Identificador único do cliente na aplicação
    /// Para rastreamento e correlação
    /// </summary>
    [JsonPropertyName("clientGuid")]
    public string ClientGuid { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp UTC de quando foi feito sync
    /// </summary>
    [JsonPropertyName("syncedAtUtc")]
    public DateTime SyncedAtUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// DTO para resposta da sincronização com VTEX Master Data API.
/// </summary>
public sealed record VtexClientSyncResponse
{
    /// <summary>
    /// ID da entidade criada/atualizada em VTEX
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>
    /// Código HTTP retornado
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int? StatusCode { get; init; }

    /// <summary>
    /// Mensagem de erro (se houver)
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>
    /// Sucesso ou falha
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }
}
