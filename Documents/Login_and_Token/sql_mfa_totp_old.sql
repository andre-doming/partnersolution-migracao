/*
  MFA obrigatório (TOTP/RFC6238) — Scripts SQL
  Banco: SQL Server

  Este script assume que já existe:
  - tb_usuario(Id)

  Observações:
  - Usamos GETDATE() (horário local do SQL Server) para consistência com o legado.
    Se o projeto preferir UTC, trocar por SYSUTCDATETIME() e usar DATETIME2.
*/

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRAN;

    /* =========================================================
       1) tb_usuario_mfa (1:1)
       ========================================================= */
    IF OBJECT_ID('dbo.tb_usuario_mfa', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.tb_usuario_mfa
        (
            UserId INT NOT NULL PRIMARY KEY,

            MfaEnabled BIT NOT NULL CONSTRAINT DF_tb_usuario_mfa_MfaEnabled DEFAULT(0),
            MfaSecret VARBINARY(512) NULL,
            MfaSetupStartedAt DATETIME NULL,
            MfaConfiguredAt DATETIME NULL,
            RecoveryCodesGeneratedAt DATETIME NULL,
            MfaResetRequired BIT NOT NULL CONSTRAINT DF_tb_usuario_mfa_MfaResetRequired DEFAULT(0),

            FailedPasswordAttempts INT NOT NULL CONSTRAINT DF_tb_usuario_mfa_FailedPasswordAttempts DEFAULT(0),
            PasswordLockoutUntil DATETIME NULL,

            FailedMfaAttempts INT NOT NULL CONSTRAINT DF_tb_usuario_mfa_FailedMfaAttempts DEFAULT(0),
            MfaLockoutUntil DATETIME NULL,

            LastSuccessfulMfaAt DATETIME NULL,

            CreatedAt DATETIME NOT NULL CONSTRAINT DF_tb_usuario_mfa_CreatedAt DEFAULT(GETDATE()),
            UpdatedAt DATETIME NULL,

            CONSTRAINT FK_tb_usuario_mfa_usuario
                FOREIGN KEY (UserId)
                REFERENCES dbo.tb_usuario(Id)
        );
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_tb_usuario_mfa_PasswordLockoutUntil'
          AND object_id = OBJECT_ID('dbo.tb_usuario_mfa')
    )
    BEGIN
        CREATE INDEX IX_tb_usuario_mfa_PasswordLockoutUntil
            ON dbo.tb_usuario_mfa(PasswordLockoutUntil);
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_tb_usuario_mfa_MfaLockoutUntil'
          AND object_id = OBJECT_ID('dbo.tb_usuario_mfa')
    )
    BEGIN
        CREATE INDEX IX_tb_usuario_mfa_MfaLockoutUntil
            ON dbo.tb_usuario_mfa(MfaLockoutUntil);
    END

    /* =========================================================
       2) tb_usuario_mfa_recovery
       ========================================================= */
    IF OBJECT_ID('dbo.tb_usuario_mfa_recovery', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.tb_usuario_mfa_recovery
        (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            UserId INT NOT NULL,
            CodeId UNIQUEIDENTIFIER NOT NULL,
            CodeHash VARCHAR(500) NOT NULL,
            Used BIT NOT NULL CONSTRAINT DF_tb_usuario_mfa_recovery_Used DEFAULT(0),
            UsedAt DATETIME NULL,
            Invalidated BIT NOT NULL CONSTRAINT DF_tb_usuario_mfa_recovery_Invalidated DEFAULT(0),
            InvalidatedAt DATETIME NULL,
            CreatedAt DATETIME NOT NULL CONSTRAINT DF_tb_usuario_mfa_recovery_CreatedAt DEFAULT(GETDATE()),

            CONSTRAINT FK_tb_usuario_mfa_recovery_usuario
                FOREIGN KEY (UserId)
                REFERENCES dbo.tb_usuario(Id)
        );
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_tb_usuario_mfa_recovery_UserId_Used'
          AND object_id = OBJECT_ID('dbo.tb_usuario_mfa_recovery')
    )
    BEGIN
        CREATE INDEX IX_tb_usuario_mfa_recovery_UserId_Used
            ON dbo.tb_usuario_mfa_recovery(UserId, Used);
    END

    /* =========================================================
       3) tb_auth_mfa_pending (token temporário de etapa 2)
       ========================================================= */
    IF OBJECT_ID('dbo.tb_auth_mfa_pending', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.tb_auth_mfa_pending
        (
            Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
            UserId INT NOT NULL,

            TokenHash VARBINARY(64) NOT NULL,
            ExpiresAt DATETIME NOT NULL,
            ConsumedAt DATETIME NULL,
            CreatedAt DATETIME NOT NULL CONSTRAINT DF_tb_auth_mfa_pending_CreatedAt DEFAULT(GETDATE()),

            IpAddress VARCHAR(45) NULL,
            UserAgentHash VARBINARY(32) NULL,

            CONSTRAINT FK_tb_auth_mfa_pending_usuario
                FOREIGN KEY (UserId)
                REFERENCES dbo.tb_usuario(Id)
        );
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_tb_auth_mfa_pending_UserId'
          AND object_id = OBJECT_ID('dbo.tb_auth_mfa_pending')
    )
    BEGIN
        CREATE INDEX IX_tb_auth_mfa_pending_UserId
            ON dbo.tb_auth_mfa_pending(UserId);
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_tb_auth_mfa_pending_ExpiresAt'
          AND object_id = OBJECT_ID('dbo.tb_auth_mfa_pending')
    )
    BEGIN
        CREATE INDEX IX_tb_auth_mfa_pending_ExpiresAt
            ON dbo.tb_auth_mfa_pending(ExpiresAt);
    END

    /* =========================================================
       4) tb_audit_log — criação/alterações
       ========================================================= */
    IF OBJECT_ID('dbo.tb_audit_log', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.tb_audit_log
        (
            Id BIGINT IDENTITY(1,1) PRIMARY KEY,
            UserId INT NULL,
            EventType VARCHAR(100) NOT NULL,
            EventDescription VARCHAR(500) NULL,
            CorrelationId VARCHAR(100) NULL,
            CreatedAt DATETIME NOT NULL CONSTRAINT DF_tb_audit_log_CreatedAt DEFAULT(GETDATE())
        );
    END

    /* Colunas opcionais recomendadas (se já existir a tabela, adiciona sem quebrar) */
    IF COL_LENGTH('dbo.tb_audit_log', 'ActorUserId') IS NULL
        ALTER TABLE dbo.tb_audit_log ADD ActorUserId INT NULL;

    IF COL_LENGTH('dbo.tb_audit_log', 'IpAddress') IS NULL
        ALTER TABLE dbo.tb_audit_log ADD IpAddress VARCHAR(45) NULL;

    IF COL_LENGTH('dbo.tb_audit_log', 'UserAgent') IS NULL
        ALTER TABLE dbo.tb_audit_log ADD UserAgent VARCHAR(300) NULL;

    IF COL_LENGTH('dbo.tb_audit_log', 'MetadataJson') IS NULL
        ALTER TABLE dbo.tb_audit_log ADD MetadataJson NVARCHAR(MAX) NULL;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_tb_audit_log_UserId_CreatedAt'
          AND object_id = OBJECT_ID('dbo.tb_audit_log')
    )
    BEGIN
        CREATE INDEX IX_tb_audit_log_UserId_CreatedAt
            ON dbo.tb_audit_log(UserId, CreatedAt);
    END

    /* =========================================================
       5) Inicialização: garantir registro em tb_usuario_mfa para usuários existentes
       ========================================================= */
    INSERT INTO dbo.tb_usuario_mfa (UserId)
    SELECT u.Id
    FROM dbo.tb_usuario u
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.tb_usuario_mfa m
        WHERE m.UserId = u.Id
    );

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();

    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
