/*
==========================================================
MFA V1 - Partner Solution
Google Authenticator / Microsoft Authenticator / FortiToken
==========================================================
*/
use db_partner
go

SET NOCOUNT ON;
GO

/*
==========================================================
TB_USUARIO_MFA
==========================================================
*/

IF OBJECT_ID('dbo.tb_usuario_mfa', 'U') IS NULL
BEGIN

    CREATE TABLE dbo.tb_usuario_mfa
    (
        UserId INT NOT NULL,

        MfaEnabled BIT NOT NULL
            CONSTRAINT DF_tb_usuario_mfa_MfaEnabled DEFAULT(0),

        MfaSecret VARBINARY(512) NULL,

        MfaSetupStartedAt DATETIME2 NULL,

        MfaConfiguredAt DATETIME2 NULL,

        RecoveryCodesGeneratedAt DATETIME2 NULL,

        MfaResetRequired BIT NOT NULL
            CONSTRAINT DF_tb_usuario_mfa_MfaResetRequired DEFAULT(0),

        FailedPasswordAttempts INT NOT NULL
            CONSTRAINT DF_tb_usuario_mfa_FailedPasswordAttempts DEFAULT(0),

        PasswordLockoutUntil DATETIME2 NULL,

        FailedMfaAttempts INT NOT NULL
            CONSTRAINT DF_tb_usuario_mfa_FailedMfaAttempts DEFAULT(0),

        MfaLockoutUntil DATETIME2 NULL,

        LastSuccessfulMfaAt DATETIME2 NULL,

        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_tb_usuario_mfa_CreatedAt DEFAULT(SYSDATETIME()),

        UpdatedAt DATETIME2 NULL,

        CONSTRAINT PK_tb_usuario_mfa
            PRIMARY KEY(UserId),

        CONSTRAINT FK_tb_usuario_mfa_tb_usuario
            FOREIGN KEY(UserId)
            REFERENCES dbo.tb_usuario(Id),

        CONSTRAINT CK_tb_usuario_mfa_FailedPasswordAttempts
            CHECK (FailedPasswordAttempts >= 0),

        CONSTRAINT CK_tb_usuario_mfa_FailedMfaAttempts
            CHECK (FailedMfaAttempts >= 0)
    );

END
GO

/*
==========================================================
INDICES TB_USUARIO_MFA
==========================================================
*/

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_tb_usuario_mfa_PasswordLockoutUntil'
)
BEGIN

    CREATE INDEX IX_tb_usuario_mfa_PasswordLockoutUntil
        ON dbo.tb_usuario_mfa(PasswordLockoutUntil);

END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_tb_usuario_mfa_MfaLockoutUntil'
)
BEGIN

    CREATE INDEX IX_tb_usuario_mfa_MfaLockoutUntil
        ON dbo.tb_usuario_mfa(MfaLockoutUntil);

END
GO

/*
==========================================================
TB_USUARIO_MFA_RECOVERY
==========================================================
*/

IF OBJECT_ID('dbo.tb_usuario_mfa_recovery', 'U') IS NULL
BEGIN

    CREATE TABLE dbo.tb_usuario_mfa_recovery
    (
        Id INT IDENTITY(1,1) NOT NULL,

        UserId INT NOT NULL,

        CodeId UNIQUEIDENTIFIER NOT NULL,

        CodeHash VARCHAR(500) NOT NULL,

        Used BIT NOT NULL
            CONSTRAINT DF_tb_usuario_mfa_recovery_Used DEFAULT(0),

        UsedAt DATETIME2 NULL,

        Invalidated BIT NOT NULL
            CONSTRAINT DF_tb_usuario_mfa_recovery_Invalidated DEFAULT(0),

        InvalidatedAt DATETIME2 NULL,

        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_tb_usuario_mfa_recovery_CreatedAt DEFAULT(SYSDATETIME()),

        CONSTRAINT PK_tb_usuario_mfa_recovery
            PRIMARY KEY(Id),

        CONSTRAINT FK_tb_usuario_mfa_recovery_tb_usuario
            FOREIGN KEY(UserId)
            REFERENCES dbo.tb_usuario(Id)
    );

END
GO

/*
==========================================================
INDICES RECOVERY CODES
==========================================================
*/

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_tb_usuario_mfa_recovery_UserId_Used'
)
BEGIN

    CREATE INDEX IX_tb_usuario_mfa_recovery_UserId_Used
        ON dbo.tb_usuario_mfa_recovery(UserId, Used);

END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_tb_usuario_mfa_recovery_Valid'
)
BEGIN

    CREATE INDEX IX_tb_usuario_mfa_recovery_Valid
        ON dbo.tb_usuario_mfa_recovery(UserId, Invalidated, Used);

END
GO

/*
==========================================================
TB_AUTH_MFA_PENDING
Fluxo:
LOGIN -> MFA_REQUIRED -> MFA_VERIFY
==========================================================
*/

IF OBJECT_ID('dbo.tb_auth_mfa_pending', 'U') IS NULL
BEGIN

    CREATE TABLE dbo.tb_auth_mfa_pending
    (
        Id UNIQUEIDENTIFIER NOT NULL,

        UserId INT NOT NULL,

        TokenHash VARBINARY(64) NOT NULL,

        ExpiresAt DATETIME2 NOT NULL,

        ConsumedAt DATETIME2 NULL,

        IpAddress VARCHAR(45) NULL,

        UserAgentHash VARBINARY(32) NULL,

        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_tb_auth_mfa_pending_CreatedAt DEFAULT(SYSDATETIME()),

        CONSTRAINT PK_tb_auth_mfa_pending
            PRIMARY KEY(Id),

        CONSTRAINT FK_tb_auth_mfa_pending_tb_usuario
            FOREIGN KEY(UserId)
            REFERENCES dbo.tb_usuario(Id)
    );

END
GO

/*
==========================================================
INDICES MFA PENDING
==========================================================
*/

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_tb_auth_mfa_pending_UserId'
)
BEGIN

    CREATE INDEX IX_tb_auth_mfa_pending_UserId
        ON dbo.tb_auth_mfa_pending(UserId);

END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_tb_auth_mfa_pending_ExpiresAt'
)
BEGIN

    CREATE INDEX IX_tb_auth_mfa_pending_ExpiresAt
        ON dbo.tb_auth_mfa_pending(ExpiresAt);

END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_tb_auth_mfa_pending_User_Consumed'
)
BEGIN

    CREATE INDEX IX_tb_auth_mfa_pending_User_Consumed
        ON dbo.tb_auth_mfa_pending(UserId, ConsumedAt);

END
GO

/*
==========================================================
TB_AUDIT_LOG
Cria apenas se ainda não existir
==========================================================
*/

IF OBJECT_ID('dbo.tb_audit_log', 'U') IS NULL
BEGIN

    CREATE TABLE dbo.tb_audit_log
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,

        UserId INT NULL,

        ActorUserId INT NULL,

        EventType VARCHAR(100) NOT NULL,

        EventDescription VARCHAR(500) NULL,

        CorrelationId VARCHAR(100) NULL,

        IpAddress VARCHAR(45) NULL,

        UserAgent VARCHAR(300) NULL,

        MetadataJson NVARCHAR(MAX) NULL,

        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_tb_audit_log_CreatedAt DEFAULT(SYSDATETIME()),

        CONSTRAINT PK_tb_audit_log
            PRIMARY KEY(Id)
    );

END
GO

/*
==========================================================
BACKFILL
Cria registro MFA para usuários já existentes
==========================================================
*/

INSERT INTO dbo.tb_usuario_mfa
(
    UserId
)
SELECT
    u.Id
FROM dbo.tb_usuario u
LEFT JOIN dbo.tb_usuario_mfa m
    ON m.UserId = u.Id
WHERE m.UserId IS NULL;
GO

/*
==========================================================
VALIDAÇÃO
==========================================================
*/

SELECT COUNT(*) AS TotalUsuarios
FROM dbo.tb_usuario;
GO

SELECT COUNT(*) AS TotalUsuariosMfa
FROM dbo.tb_usuario_mfa;
GO

PRINT 'MFA V1 instalado com sucesso.';
GO