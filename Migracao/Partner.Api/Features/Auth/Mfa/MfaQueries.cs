namespace Partner.Api.Features.Auth.Mfa;

public static class MfaQueries
{
    public const string EnsureMfaTables = """
        IF OBJECT_ID('dbo.tb_usuario_mfa', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.tb_usuario_mfa
            (
                UserId INT PRIMARY KEY,
                MfaEnabled BIT NOT NULL DEFAULT 0,
                MfaSecret NVARCHAR(100) NULL,
                MfaSetupStartedAt DATETIME2 NULL,
                MfaConfiguredAt DATETIME2 NULL,
                MfaResetRequired BIT NOT NULL DEFAULT 0,
                RecoveryCodesGeneratedAt DATETIME2 NULL,
                FailedPasswordAttempts INT NOT NULL DEFAULT 0,
                PasswordLockoutUntil DATETIME2 NULL,
                FailedMfaAttempts INT NOT NULL DEFAULT 0,
                MfaLockoutUntil DATETIME2 NULL,
                LastSuccessfulMfaAt DATETIME2 NULL,
                CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            );
        END;

        IF OBJECT_ID('dbo.tb_usuario_mfa_recovery', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.tb_usuario_mfa_recovery
            (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                UserId INT NOT NULL,
                CodeId UNIQUEIDENTIFIER NOT NULL,
                CodeHash NVARCHAR(256) NOT NULL,
                Used BIT NOT NULL DEFAULT 0,
                UsedAt DATETIME2 NULL,
                Invalidated BIT NOT NULL DEFAULT 0,
                InvalidatedAt DATETIME2 NULL,
                CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            );
        END;

        IF OBJECT_ID('dbo.tb_auth_mfa_pending', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.tb_auth_mfa_pending
            (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                UserId INT NOT NULL,
                TokenHash NVARCHAR(256) NOT NULL,
                ExpiresAt DATETIME2 NOT NULL,
                ConsumedAt DATETIME2 NULL,
                IpAddress NVARCHAR(45) NULL,
                UserAgentHash VARBINARY(32) NULL,
                CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            );
        END;

        IF OBJECT_ID('dbo.tb_audit_log', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.tb_audit_log
            (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                UserId INT NOT NULL,
                ActorUserId INT NOT NULL,
                EventType NVARCHAR(50) NOT NULL,
                EventDescription NVARCHAR(500) NOT NULL,
                CorrelationId NVARCHAR(100) NOT NULL,
                IpAddress NVARCHAR(45) NULL,
                UserAgent NVARCHAR(500) NULL,
                MetadataJson NVARCHAR(MAX) NULL,
                CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            );
        END;
        """;

    public const string GetMfaStateByUserId = """
        SELECT
            m.UserId AS UserId,
            m.MfaEnabled AS MfaEnabled,
            m.MfaSecret AS MfaSecret,
            m.MfaSetupStartedAt AS MfaSetupStartedAt,
            m.MfaConfiguredAt AS MfaConfiguredAt,
            m.RecoveryCodesGeneratedAt AS RecoveryCodesGeneratedAt,
            m.MfaResetRequired AS MfaResetRequired,
            m.FailedPasswordAttempts AS FailedPasswordAttempts,
            m.PasswordLockoutUntil AS PasswordLockoutUntil,
            m.FailedMfaAttempts AS FailedMfaAttempts,
            m.MfaLockoutUntil AS MfaLockoutUntil,
            m.LastSuccessfulMfaAt AS LastSuccessfulMfaAt,
            m.CreatedAt AS CreatedAt,
            m.UpdatedAt AS UpdatedAt
        FROM dbo.tb_usuario_mfa m
        WHERE m.UserId = @UserId;
        """;

    public const string EnsureMfaRowForUser = """
        IF NOT EXISTS (SELECT 1 FROM dbo.tb_usuario_mfa WHERE UserId = @UserId)
        BEGIN
            INSERT INTO dbo.tb_usuario_mfa (UserId)
            VALUES (@UserId);
        END
        """;

    public const string UpdateMfaSetupStarted = """
        UPDATE dbo.tb_usuario_mfa
        SET
            MfaSecret = @MfaSecret,
            MfaSetupStartedAt = @MfaSetupStartedAt,
            UpdatedAt = @UpdatedAt
        WHERE UserId = @UserId;
        """;

    public const string EnableMfa = """
        UPDATE dbo.tb_usuario_mfa
        SET
            MfaEnabled = 1,
            MfaConfiguredAt = @MfaConfiguredAt,
            MfaResetRequired = 0,
            RecoveryCodesGeneratedAt = @RecoveryCodesGeneratedAt,
            UpdatedAt = @UpdatedAt
        WHERE UserId = @UserId;
        """;

    public const string RequireMfaReset = """
        UPDATE dbo.tb_usuario_mfa
        SET
            MfaEnabled = 0,
            MfaSecret = NULL,
            MfaConfiguredAt = NULL,
            MfaResetRequired = 1,
            RecoveryCodesGeneratedAt = NULL,
            UpdatedAt = @UpdatedAt
        WHERE UserId = @UserId;
        """;

    public const string AdminResetMfa = """
        UPDATE dbo.tb_usuario_mfa
        SET
            MfaEnabled = 0,
            MfaSecret = NULL,
            MfaConfiguredAt = NULL,
            LastSuccessfulMfaAt = NULL,
            MfaResetRequired = 1,
            FailedMfaAttempts = 0,
            MfaLockoutUntil = NULL,
            RecoveryCodesGeneratedAt = NULL,
            UpdatedAt = @UpdatedAt
        WHERE UserId = @UserId;
        """;

    public const string UpdateLastSuccessfulMfa = """
        UPDATE dbo.tb_usuario_mfa
        SET
            LastSuccessfulMfaAt = @LastSuccessfulMfaAt,
            UpdatedAt = @UpdatedAt
        WHERE UserId = @UserId;
        """;

    public const string UpdatePasswordLockout = """
        UPDATE dbo.tb_usuario_mfa
        SET
            FailedPasswordAttempts = @FailedPasswordAttempts,
            PasswordLockoutUntil = @PasswordLockoutUntil,
            UpdatedAt = @UpdatedAt
        WHERE UserId = @UserId;
        """;

    public const string UpdateMfaLockout = """
        UPDATE dbo.tb_usuario_mfa
        SET
            FailedMfaAttempts = @FailedMfaAttempts,
            MfaLockoutUntil = @MfaLockoutUntil,
            UpdatedAt = @UpdatedAt
        WHERE UserId = @UserId;
        """;

    public const string ResetPasswordLockout = """
        UPDATE dbo.tb_usuario_mfa
        SET
            FailedPasswordAttempts = 0,
            PasswordLockoutUntil = NULL,
            UpdatedAt = @UpdatedAt
        WHERE UserId = @UserId;
        """;

    public const string ResetMfaLockout = """
        UPDATE dbo.tb_usuario_mfa
        SET
            FailedMfaAttempts = 0,
            MfaLockoutUntil = NULL,
            UpdatedAt = @UpdatedAt
        WHERE UserId = @UserId;
        """;

    public const string InsertRecoveryCode = """
        INSERT INTO dbo.tb_usuario_mfa_recovery
        (
            UserId,
            CodeId,
            CodeHash,
            Used,
            Invalidated,
            CreatedAt
        )
        VALUES
        (
            @UserId,
            @CodeId,
            @CodeHash,
            0,
            0,
            @CreatedAt
        );
        """;

    public const string ListValidRecoveryCodes = """
        SELECT
            r.Id AS Id,
            r.UserId AS UserId,
            r.CodeId AS CodeId,
            r.CodeHash AS CodeHash,
            r.Used AS Used,
            r.UsedAt AS UsedAt,
            r.Invalidated AS Invalidated,
            r.InvalidatedAt AS InvalidatedAt,
            r.CreatedAt AS CreatedAt
        FROM dbo.tb_usuario_mfa_recovery r
        WHERE r.UserId = @UserId
          AND r.Invalidated = 0
          AND r.Used = 0;
        """;

    public const string MarkRecoveryCodeUsed = """
        UPDATE dbo.tb_usuario_mfa_recovery
        SET
            Used = 1,
            UsedAt = @UsedAt
        WHERE Id = @Id
          AND UserId = @UserId;
        """;

    public const string InvalidateRecoveryCodes = """
        UPDATE dbo.tb_usuario_mfa_recovery
        SET
            Invalidated = 1,
            InvalidatedAt = @InvalidatedAt
        WHERE UserId = @UserId
          AND Invalidated = 0;
        """;

    public const string InsertPendingSession = """
        INSERT INTO dbo.tb_auth_mfa_pending
        (
            Id,
            UserId,
            TokenHash,
            ExpiresAt,
            ConsumedAt,
            IpAddress,
            UserAgentHash,
            CreatedAt
        )
        VALUES
        (
            @Id,
            @UserId,
            @TokenHash,
            @ExpiresAt,
            NULL,
            @IpAddress,
            @UserAgentHash,
            @CreatedAt
        );
        """;

    public const string GetPendingSessionById = """
        SELECT
            p.Id AS Id,
            p.UserId AS UserId,
            p.TokenHash AS TokenHash,
            p.ExpiresAt AS ExpiresAt,
            p.ConsumedAt AS ConsumedAt,
            p.IpAddress AS IpAddress,
            p.UserAgentHash AS UserAgentHash,
            p.CreatedAt AS CreatedAt
        FROM dbo.tb_auth_mfa_pending p
        WHERE p.Id = @Id;
        """;

    public const string MarkPendingSessionConsumed = """
        UPDATE dbo.tb_auth_mfa_pending
        SET
            ConsumedAt = @ConsumedAt
        WHERE Id = @Id
          AND ConsumedAt IS NULL;
        """;

    public const string DeletePendingSession = """
        DELETE FROM dbo.tb_auth_mfa_pending
        WHERE Id = @Id;
        """;

    public const string DeletePendingSessionsByUser = """
        DELETE FROM dbo.tb_auth_mfa_pending
        WHERE UserId = @UserId;
        """;

    public const string DeleteRecoveryCodesByUser = """
        DELETE FROM dbo.tb_usuario_mfa_recovery
        WHERE UserId = @UserId;
        """;

    public const string InsertAuditLog = """
        INSERT INTO dbo.tb_audit_log
        (
            UserId,
            ActorUserId,
            EventType,
            EventDescription,
            CorrelationId,
            IpAddress,
            UserAgent,
            MetadataJson,
            CreatedAt
        )
        VALUES
        (
            @UserId,
            @ActorUserId,
            @EventType,
            @EventDescription,
            @CorrelationId,
            @IpAddress,
            @UserAgent,
            @MetadataJson,
            @CreatedAt
        );
        """;
}