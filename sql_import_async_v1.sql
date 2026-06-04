-- IMP-1: Fundação da Importação Assíncrona (RabbitMQ)
-- Script versionado (não executar automaticamente)

IF OBJECT_ID('dbo.ImportJobs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ImportJobs
    (
        id INT IDENTITY(1,1) PRIMARY KEY,
        public_id UNIQUEIDENTIFIER NOT NULL,
        feature NVARCHAR(60) NOT NULL,
        file_name NVARCHAR(260) NOT NULL,
        file_path NVARCHAR(500) NOT NULL,
        file_hash_sha256 CHAR(64) NOT NULL,
        company_id INT NOT NULL,
        status NVARCHAR(30) NOT NULL,
        total_rows INT NULL,
        processed_rows INT NOT NULL,
        success_rows INT NOT NULL,
        error_rows INT NOT NULL,
        duration_ms INT NOT NULL,
        started_at_utc DATETIME2 NOT NULL,
        finished_at_utc DATETIME2 NULL,
        created_by_user_id INT NOT NULL,
        cancel_requested BIT NOT NULL,
        cancel_requested_at_utc DATETIME2 NULL,
        attempts INT NOT NULL,
        last_error NVARCHAR(2000) NULL,
        locked_by NVARCHAR(100) NULL,
        locked_at_utc DATETIME2 NULL,
        last_heartbeat_at_utc DATETIME2 NULL,
        correlation_id NVARCHAR(100) NULL,
        cancelled_at_utc DATETIME2 NULL,
        retry_of_import_job_id INT NULL
    );

    CREATE UNIQUE INDEX UQ_ImportJobs_PublicId ON dbo.ImportJobs(public_id);
    CREATE INDEX IX_ImportJobs_Company_Status_CreatedAt ON dbo.ImportJobs(company_id, status, started_at_utc DESC);
    CREATE INDEX IX_ImportJobs_FileHash_Window ON dbo.ImportJobs(company_id, feature, file_hash_sha256, started_at_utc DESC);
END;

IF OBJECT_ID('dbo.ImportJobErrors', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ImportJobErrors
    (
        id INT IDENTITY(1,1) PRIMARY KEY,
        import_job_id INT NOT NULL,
        seq INT NOT NULL,
        line_number INT NOT NULL,
        error_code NVARCHAR(50) NULL,
        message NVARCHAR(1000) NOT NULL,
        raw_line NVARCHAR(MAX) NULL,
        action NVARCHAR(30) NULL,
        document NVARCHAR(30) NULL,
        email NVARCHAR(150) NULL,
        created_at_utc DATETIME2 NOT NULL,
        CONSTRAINT FK_ImportJobErrors_ImportJobs FOREIGN KEY (import_job_id) REFERENCES dbo.ImportJobs(id)
    );

    CREATE INDEX IX_ImportJobErrors_Job_Seq ON dbo.ImportJobErrors(import_job_id, seq);
END;

IF OBJECT_ID('dbo.ImportNotifications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ImportNotifications
    (
        id INT IDENTITY(1,1) PRIMARY KEY,
        import_job_id INT NOT NULL,
        user_id INT NOT NULL,
        title NVARCHAR(200) NOT NULL,
        message NVARCHAR(1000) NOT NULL,
        status NVARCHAR(20) NOT NULL,
        created_at_utc DATETIME2 NOT NULL,
        read_at_utc DATETIME2 NULL,
        CONSTRAINT FK_ImportNotifications_ImportJobs FOREIGN KEY (import_job_id) REFERENCES dbo.ImportJobs(id)
    );

    CREATE INDEX IX_ImportNotifications_User_Status_CreatedAt ON dbo.ImportNotifications(user_id, status, created_at_utc DESC);
END;

IF OBJECT_ID('dbo.ImportJobItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ImportJobItems
    (
        id INT IDENTITY(1,1) PRIMARY KEY,
        import_job_id INT NOT NULL,
        seq INT NOT NULL,
        line_hash CHAR(64) NOT NULL,
        status NVARCHAR(30) NOT NULL,
        processed_at_utc DATETIME2 NOT NULL,
        error_id INT NULL,
        target_key NVARCHAR(100) NULL,
        CONSTRAINT FK_ImportJobItems_ImportJobs FOREIGN KEY (import_job_id) REFERENCES dbo.ImportJobs(id)
    );

    CREATE UNIQUE INDEX UQ_ImportJobItems_Job_Seq ON dbo.ImportJobItems(import_job_id, seq);
END;