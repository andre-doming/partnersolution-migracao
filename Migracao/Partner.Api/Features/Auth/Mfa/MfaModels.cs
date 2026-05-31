namespace Partner.Api.Features.Auth.Mfa;

public sealed class UserMfaState
{
    public int UserId { get; init; }
    public bool MfaEnabled { get; init; }
    public byte[]? MfaSecret { get; init; }
    public DateTime? MfaSetupStartedAt { get; init; }
    public DateTime? MfaConfiguredAt { get; init; }
    public DateTime? RecoveryCodesGeneratedAt { get; init; }
    public bool MfaResetRequired { get; init; }
    public int FailedPasswordAttempts { get; init; }
    public DateTime? PasswordLockoutUntil { get; init; }
    public int FailedMfaAttempts { get; init; }
    public DateTime? MfaLockoutUntil { get; init; }
    public DateTime? LastSuccessfulMfaAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class UserMfaRecoveryCode
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public Guid CodeId { get; init; }
    public string CodeHash { get; init; } = string.Empty;
    public bool Used { get; init; }
    public DateTime? UsedAt { get; init; }
    public bool Invalidated { get; init; }
    public DateTime? InvalidatedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class MfaPendingSession
{
    public Guid Id { get; init; }
    public int UserId { get; init; }
    public byte[] TokenHash { get; init; } = [];
    public DateTime ExpiresAt { get; init; }
    public DateTime? ConsumedAt { get; init; }
    public string? IpAddress { get; init; }
    public byte[]? UserAgentHash { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class MfaAuditLogEntry
{
    public long Id { get; init; }
    public int? UserId { get; init; }
    public int? ActorUserId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string? EventDescription { get; init; }
    public string? CorrelationId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? MetadataJson { get; init; }
    public DateTime CreatedAt { get; init; }
}