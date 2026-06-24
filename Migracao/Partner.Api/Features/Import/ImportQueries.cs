namespace Partner.Api.Features.Import;

public static class ImportQueries
{
    public const string EnsureImportTables = """
        IF OBJECT_ID('dbo.ImportJobs', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ImportJobs
            (
                id INT IDENTITY(1,1) PRIMARY KEY,
                public_id UNIQUEIDENTIFIER NOT NULL UNIQUE,
                feature NVARCHAR(60) NOT NULL,
                file_name NVARCHAR(260) NOT NULL,
                file_path NVARCHAR(500) NOT NULL,
                file_hash_sha256 NVARCHAR(64) NOT NULL,
                company_id INT NOT NULL,
                status NVARCHAR(30) NOT NULL,
                total_rows INT NOT NULL,
                processed_rows INT NOT NULL DEFAULT 0,
                success_rows INT NOT NULL,
                error_rows INT NOT NULL,
                duration_ms INT NOT NULL,
                started_at_utc DATETIME2 NOT NULL,
                finished_at_utc DATETIME2 NULL,
                created_by_user_id INT NOT NULL,
                created_at_utc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                attempts INT NOT NULL DEFAULT 0,
                last_error NVARCHAR(MAX) NULL,
                locked_by NVARCHAR(100) NULL,
                locked_at_utc DATETIME2 NULL,
                last_heartbeat_at_utc DATETIME2 NULL,
                correlation_id NVARCHAR(100) NOT NULL,
                cancel_requested BIT NOT NULL DEFAULT 0,
                cancel_requested_at_utc DATETIME2 NULL,
                cancelled_at_utc DATETIME2 NULL,
                retry_of_import_job_id INT NULL
            );
        END
        ELSE
        BEGIN
            -- Add missing columns if they don't exist
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='public_id')
                ALTER TABLE dbo.ImportJobs ADD public_id UNIQUEIDENTIFIER NOT NULL UNIQUE DEFAULT NEWID();
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='file_path')
                ALTER TABLE dbo.ImportJobs ADD file_path NVARCHAR(500) NOT NULL DEFAULT '';
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='file_hash_sha256')
                ALTER TABLE dbo.ImportJobs ADD file_hash_sha256 NVARCHAR(64) NOT NULL DEFAULT '';
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='processed_rows')
                ALTER TABLE dbo.ImportJobs ADD processed_rows INT NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='attempts')
                ALTER TABLE dbo.ImportJobs ADD attempts INT NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='last_error')
                ALTER TABLE dbo.ImportJobs ADD last_error NVARCHAR(MAX) NULL;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='locked_by')
                ALTER TABLE dbo.ImportJobs ADD locked_by NVARCHAR(100) NULL;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='locked_at_utc')
                ALTER TABLE dbo.ImportJobs ADD locked_at_utc DATETIME2 NULL;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='last_heartbeat_at_utc')
                ALTER TABLE dbo.ImportJobs ADD last_heartbeat_at_utc DATETIME2 NULL;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='correlation_id')
                ALTER TABLE dbo.ImportJobs ADD correlation_id NVARCHAR(100) NOT NULL DEFAULT '';
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='cancel_requested')
                ALTER TABLE dbo.ImportJobs ADD cancel_requested BIT NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='cancel_requested_at_utc')
                ALTER TABLE dbo.ImportJobs ADD cancel_requested_at_utc DATETIME2 NULL;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='cancelled_at_utc')
                ALTER TABLE dbo.ImportJobs ADD cancelled_at_utc DATETIME2 NULL;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='retry_of_import_job_id')
                ALTER TABLE dbo.ImportJobs ADD retry_of_import_job_id INT NULL;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='created_at_utc')
                ALTER TABLE dbo.ImportJobs ADD created_at_utc DATETIME2 NOT NULL DEFAULT GETUTCDATE();
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
                action NVARCHAR(30) NULL,
                document NVARCHAR(30) NULL,
                email NVARCHAR(150) NULL,
                message NVARCHAR(500) NOT NULL,
                raw_line NVARCHAR(MAX) NULL,
                created_at_utc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                CONSTRAINT FK_ImportJobErrors_ImportJobs FOREIGN KEY (import_job_id) REFERENCES dbo.ImportJobs(id)
            );
        END
        ELSE
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobErrors' AND COLUMN_NAME='seq')
                ALTER TABLE dbo.ImportJobErrors ADD seq INT NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobErrors' AND COLUMN_NAME='error_code')
                ALTER TABLE dbo.ImportJobErrors ADD error_code NVARCHAR(50) NULL;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobErrors' AND COLUMN_NAME='raw_line')
                ALTER TABLE dbo.ImportJobErrors ADD raw_line NVARCHAR(MAX) NULL;
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobErrors' AND COLUMN_NAME='created_at_utc')
                ALTER TABLE dbo.ImportJobErrors ADD created_at_utc DATETIME2 NOT NULL DEFAULT GETUTCDATE();
        END;

        IF OBJECT_ID('dbo.ImportJobItems', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ImportJobItems
            (
                id INT IDENTITY(1,1) PRIMARY KEY,
                import_job_id INT NOT NULL,
                seq INT NOT NULL,
                line_hash NVARCHAR(64) NOT NULL,
                status NVARCHAR(30) NOT NULL,
                processed_at_utc DATETIME2 NULL,
                error_id INT NULL,
                target_key NVARCHAR(100) NULL,
                CONSTRAINT FK_ImportJobItems_ImportJobs FOREIGN KEY (import_job_id) REFERENCES dbo.ImportJobs(id),
                CONSTRAINT UK_ImportJobItems UNIQUE (import_job_id, seq)
            );
        END;

        IF OBJECT_ID('dbo.ImportNotifications', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ImportNotifications
            (
                id INT IDENTITY(1,1) PRIMARY KEY,
                import_job_id INT NOT NULL,
                user_id INT NOT NULL,
                title NVARCHAR(200) NOT NULL,
                message NVARCHAR(MAX) NOT NULL,
                status NVARCHAR(30) NOT NULL,
                created_at_utc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                read_at_utc DATETIME2 NULL,
                CONSTRAINT FK_ImportNotifications_ImportJobs FOREIGN KEY (import_job_id) REFERENCES dbo.ImportJobs(id)
            );
        END
        ELSE
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportNotifications' AND COLUMN_NAME='message')
                ALTER TABLE dbo.ImportNotifications ADD message NVARCHAR(MAX) NOT NULL DEFAULT '';
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportNotifications' AND COLUMN_NAME='status')
                ALTER TABLE dbo.ImportNotifications ADD status NVARCHAR(30) NOT NULL DEFAULT 'Unread';
        END;
        """;

    public const string InsertImportJob = """
        INSERT INTO dbo.ImportJobs
        (
            public_id,
            feature,
            file_name,
            file_path,
            file_hash_sha256,
            company_id,
            status,
            total_rows,
            processed_rows,
            success_rows,
            error_rows,
            duration_ms,
            started_at_utc,
            finished_at_utc,
            created_by_user_id,
            created_at_utc,
            attempts,
            last_error,
            locked_by,
            locked_at_utc,
            last_heartbeat_at_utc,
            correlation_id,
            cancel_requested,
            cancel_requested_at_utc,
            cancelled_at_utc,
            retry_of_import_job_id
        )
        VALUES
        (
            @PublicId,
            @Feature,
            @FileName,
            @FilePath,
            @FileHashSha256,
            @CompanyId,
            @Status,
            @TotalRows,
            @ProcessedRows,
            @SuccessRows,
            @ErrorRows,
            @DurationMs,
            @StartedAtUtc,
            @FinishedAtUtc,
            @CreatedByUserId,
            @CreatedAtUtc,
            @Attempts,
            @LastError,
            @LockedBy,
            @LockedAtUtc,
            @LastHeartbeatAtUtc,
            @CorrelationId,
            @CancelRequested,
            @CancelRequestedAtUtc,
            @CancelledAtUtc,
            @RetryOfImportJobId
        );

        SELECT CAST(SCOPE_IDENTITY() AS int);
        """;

    public const string UpdateImportJob = """
        UPDATE dbo.ImportJobs
        SET
            status = @Status,
            total_rows = @TotalRows,
            success_rows = @SuccessRows,
            error_rows = @ErrorRows,
            duration_ms = @DurationMs,
            finished_at_utc = @FinishedAtUtc
        WHERE id = @Id;
        """;

    public const string InsertImportJobError = """
        INSERT INTO dbo.ImportJobErrors
        (
            import_job_id,
            line_number,
            action,
            document,
            email,
            message
        )
        VALUES
        (
            @ImportJobId,
            @LineNumber,
            @Action,
            @Document,
            @Email,
            @Message
        );
        """;

    public const string ListImportJobs = """
        SELECT
            j.id AS Id,
            j.public_id AS JobPublicId,
            j.feature AS Feature,
            j.file_name AS FileName,
            j.company_id AS CompanyId,
            j.status AS Status,
            j.total_rows AS TotalRows,
            j.processed_rows AS ProcessedRows,
            j.success_rows AS SuccessRows,
            j.error_rows AS ErrorRows,
            j.duration_ms AS DurationMs,
            j.created_at_utc AS CreatedAtUtc,
            j.started_at_utc AS StartedAtUtc,
            j.finished_at_utc AS FinishedAtUtc,
            j.created_by_user_id AS CreatedByUserId
        FROM dbo.ImportJobs j
        /**where**/
        ORDER BY j.id DESC
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;
        """;

    public const string CountImportJobs = """
        SELECT COUNT(1)
        FROM dbo.ImportJobs j
        /**where**/;
        """;

    public const string GetImportJobById = """
        SELECT
            j.id AS Id,
            j.feature AS Feature,
            j.file_name AS FileName,
            j.company_id AS CompanyId,
            j.status AS Status,
            j.total_rows AS TotalRows,
            j.success_rows AS SuccessRows,
            j.error_rows AS ErrorRows,
            j.duration_ms AS DurationMs,
            j.started_at_utc AS StartedAtUtc,
            j.finished_at_utc AS FinishedAtUtc,
            j.created_by_user_id AS CreatedByUserId
        FROM dbo.ImportJobs j
        WHERE j.id = @Id;
        """;

    public const string GetImportErrorsByJobId = """
        SELECT
            e.line_number AS LineNumber,
            e.action AS Action,
            e.document AS Document,
            e.email AS Email,
            e.message AS Message
        FROM dbo.ImportJobErrors e
        WHERE e.import_job_id = @ImportJobId
        ORDER BY e.line_number;
        """;

    public const string GetCompanyMapById = """
        SELECT
            e.id AS CompanyId,
            e.id_parceiro AS PartnerId,
            e.nome_fantasia AS CompanyName
        FROM tb_empresa e
        WHERE e.id = @CompanyId
          AND e.ativo = 'S';
        """;

    public const string CountUserAccessToCompany = """
        SELECT COUNT(1)
        FROM tb_empresa_usuario eu
        WHERE eu.id_usuario = @UserId
          AND eu.id_empresa = @CompanyId;
        """;

    public const string FindActiveClientByDocumentOrEmail = """
        SELECT TOP 1 c.id AS Id
        FROM tb_cliente c
        WHERE c.id_parceiro = @PartnerId
          AND c.ativo = 'S'
          AND (
            (@Document <> '' AND (
                REPLACE(REPLACE(REPLACE(c.cpf, '.', ''), '-', ''), '/', '') = @Document
                OR LOWER(c.cpf) = LOWER(CONVERT(varchar(32), HASHBYTES('MD5', @Document), 2))
            ))
            OR (@Email <> '' AND c.email = @Email)
          )
        ORDER BY c.id DESC;
        """;

    public const string InsertClient = """
        INSERT INTO tb_cliente
        (
            nome,
            sobrenome,
            cpf,
            email,
            registro_colaborador,
            id_parceiro,
            ativo,
            id_cliente
        )
        VALUES
        (
            @FirstName,
            @LastName,
            @DocumentHash,
            @Email,
            @Registration,
            @PartnerId,
            'S',
            @ClientGuid
        );
        """;

    public const string UpdateClient = """
        UPDATE tb_cliente
        SET
            nome = @FirstName,
            sobrenome = @LastName,
            cpf = @DocumentHash,
            email = @Email,
            registro_colaborador = @Registration,
            id_parceiro = @PartnerId,
            ativo = 'S'
        WHERE id = @Id;
        """;

    public const string InactivateClient = """
        UPDATE tb_cliente
        SET
            ativo = 'N'
        WHERE id = @Id;
        """;

    public const string GetImportJobByPublicId = """
        SELECT
            j.id AS Id,
            j.public_id AS PublicId,
            j.feature AS Feature,
            j.file_name AS FileName,
            j.file_path AS FilePath,
            j.file_hash_sha256 AS FileHashSha256,
            j.company_id AS CompanyId,
            j.status AS Status,
            j.total_rows AS TotalRows,
            j.processed_rows AS ProcessedRows,
            j.success_rows AS SuccessRows,
            j.error_rows AS ErrorRows,
            j.duration_ms AS DurationMs,
            j.started_at_utc AS StartedAtUtc,
            j.finished_at_utc AS FinishedAtUtc,
            j.created_by_user_id AS CreatedByUserId,
            j.attempts AS Attempts,
            j.last_error AS LastError,
            j.correlation_id AS CorrelationId,
            j.cancel_requested AS CancelRequested,
            j.cancel_requested_at_utc AS CancelRequestedAtUtc,
            j.cancelled_at_utc AS CancelledAtUtc,
            j.retry_of_import_job_id AS RetryOfImportJobId
        FROM dbo.ImportJobs j
        WHERE j.public_id = @PublicId;
        """;

    public const string MarkImportJobRunning = """
        UPDATE dbo.ImportJobs
        SET
            status = @Status,
            locked_by = @LockedBy,
            locked_at_utc = @LockedAtUtc,
            last_heartbeat_at_utc = @LockedAtUtc
        WHERE id = @Id
          AND status = @ExpectedStatus;
        """;

    public const string UpdateImportJobStatus = """
        UPDATE dbo.ImportJobs
        SET
            status = @Status,
            finished_at_utc = @FinishedAtUtc,
            duration_ms = @DurationMs,
            last_error = @LastError,
            cancelled_at_utc = @CancelledAtUtc,
            cancel_requested = @CancelRequested,
            cancel_requested_at_utc = @CancelRequestedAtUtc,
            locked_by = NULL,
            locked_at_utc = NULL,
            last_heartbeat_at_utc = @FinishedAtUtc
        WHERE id = @Id;
        """;

    public const string InsertImportNotification = """
        INSERT INTO dbo.ImportNotifications
        (
            import_job_id,
            user_id,
            title,
            message,
            status,
            created_at_utc,
            read_at_utc
        )
        VALUES
        (
            @ImportJobId,
            @UserId,
            @Title,
            @Message,
            @Status,
            @CreatedAtUtc,
            @ReadAtUtc
        );

        SELECT CAST(SCOPE_IDENTITY() AS int);
        """;

    public const string TryInsertImportJobItem = """
        INSERT INTO dbo.ImportJobItems
        (
            import_job_id,
            seq,
            line_hash,
            status,
            processed_at_utc,
            error_id,
            target_key
        )
        SELECT
            @ImportJobId,
            @Seq,
            @LineHash,
            @Status,
            @ProcessedAtUtc,
            @ErrorId,
            @TargetKey
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.ImportJobItems
            WHERE import_job_id = @ImportJobId
              AND seq = @Seq
        );

        SELECT @@ROWCOUNT;
        """;

    public const string UpdateImportJobItem = """
        UPDATE dbo.ImportJobItems
        SET
            status = @Status,
            processed_at_utc = @ProcessedAtUtc,
            error_id = @ErrorId,
            target_key = @TargetKey
        WHERE import_job_id = @ImportJobId
          AND seq = @Seq;
        """;

    public const string InsertImportJobErrorWithId = """
        INSERT INTO dbo.ImportJobErrors
        (
            import_job_id,
            seq,
            line_number,
            error_code,
            message,
            raw_line,
            action,
            document,
            email,
            created_at_utc
        )
        VALUES
        (
            @ImportJobId,
            @Seq,
            @LineNumber,
            @ErrorCode,
            @Message,
            @RawLine,
            @Action,
            @Document,
            @Email,
            @CreatedAtUtc
        );

        SELECT CAST(SCOPE_IDENTITY() AS int);
        """;

    public const string UpdateImportJobProgress = """
        UPDATE dbo.ImportJobs
        SET
            processed_rows = @ProcessedRows,
            success_rows = @SuccessRows,
            error_rows = @ErrorRows,
            last_heartbeat_at_utc = @LastHeartbeatAtUtc
        WHERE id = @Id;
        """;

    public const string GetImportJobCancellationStatus = """
        SELECT
            j.cancel_requested AS CancelRequested,
            j.status AS Status
        FROM dbo.ImportJobs j
        WHERE j.id = @Id;
        """;

    public const string UpdateImportJobCancellation = """
        UPDATE dbo.ImportJobs
        SET
            status = @Status,
            cancel_requested = @CancelRequested,
            cancel_requested_at_utc = @CancelRequestedAtUtc,
            cancelled_at_utc = @CancelledAtUtc,
            last_heartbeat_at_utc = @CancelledAtUtc
        WHERE id = @Id;
        """;

    public const string ListImportNotifications = """
        SELECT
            n.id AS Id,
            n.import_job_id AS ImportJobId,
            n.user_id AS UserId,
            n.title AS Title,
            n.message AS Message,
            n.status AS Status,
            n.created_at_utc AS CreatedAtUtc,
            n.read_at_utc AS ReadAtUtc,
            j.public_id AS ImportJobPublicId
        FROM dbo.ImportNotifications n
        JOIN dbo.ImportJobs j ON n.import_job_id = j.id
        WHERE n.user_id = @UserId
        ORDER BY n.created_at_utc DESC;
        """;

    public const string GetImportNotificationById = """
        SELECT
            n.id AS Id,
            n.import_job_id AS ImportJobId,
            n.user_id AS UserId,
            n.title AS Title,
            n.message AS Message,
            n.status AS Status,
            n.created_at_utc AS CreatedAtUtc,
            n.read_at_utc AS ReadAtUtc,
            j.public_id AS ImportJobPublicId
        FROM dbo.ImportNotifications n
        JOIN dbo.ImportJobs j ON n.import_job_id = j.id
        WHERE n.id = @Id
          AND n.user_id = @UserId;
        """;

    public const string MarkImportNotificationRead = """
        UPDATE dbo.ImportNotifications
        SET
            status = @Status,
            read_at_utc = @ReadAtUtc
        WHERE id = @Id
          AND user_id = @UserId
          AND status = @UnreadStatus;
        """;

    public const string MarkAllImportNotificationsRead = """
        UPDATE dbo.ImportNotifications
        SET
            status = @Status,
            read_at_utc = @ReadAtUtc
        WHERE user_id = @UserId
          AND status = @UnreadStatus;
        """;

    public const string FindJobByIdempotencyKey = """
        SELECT TOP 1
            j.id AS Id,
            j.public_id AS PublicId,
            j.status AS Status,
            j.started_at_utc AS StartedAtUtc
        FROM dbo.ImportJobs j
        WHERE j.company_id = @CompanyId
          AND j.feature = @Feature
          AND j.file_hash_sha256 = @FileHashSha256
        ORDER BY j.started_at_utc DESC;
        """;

    public const string ListImportErrors = """
        SELECT
            e.id AS Id,
            e.import_job_id AS ImportJobId,
            e.seq AS Seq,
            e.line_number AS LineNumber,
            e.error_code AS ErrorCode,
            e.message AS Message,
            e.raw_line AS RawLine,
            e.action AS Action,
            e.document AS Document,
            e.email AS Email,
            e.created_at_utc AS CreatedAtUtc
        FROM dbo.ImportJobErrors e
        WHERE e.import_job_id = @ImportJobId
        ORDER BY e.seq ASC
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;
        """;

    public const string CountImportErrors = """
        SELECT COUNT(1)
        FROM dbo.ImportJobErrors
        WHERE import_job_id = @ImportJobId;
        """;

    public const string RequestImportJobCancellation = """
        UPDATE dbo.ImportJobs
        SET
            status = @Status,
            cancel_requested = @CancelRequested,
            cancel_requested_at_utc = @CancelRequestedAtUtc
        WHERE id = @Id
          AND status IN ('Queued', 'Running');
        """;

    public const string GetImportJobByPublicIdDetailed = """
        SELECT
            j.id AS Id,
            j.public_id AS JobPublicId,
            j.feature AS Feature,
            j.file_name AS FileName,
            j.file_path AS FilePath,
            j.file_hash_sha256 AS FileHashSha256,
            j.company_id AS CompanyId,
            j.status AS Status,
            j.total_rows AS TotalRows,
            j.processed_rows AS ProcessedRows,
            j.success_rows AS SuccessRows,
            j.error_rows AS ErrorRows,
            j.duration_ms AS DurationMs,
            j.started_at_utc AS StartedAtUtc,
            j.finished_at_utc AS FinishedAtUtc,
            j.created_by_user_id AS CreatedByUserId,
            j.attempts AS Attempts,
            j.last_error AS LastError,
            j.correlation_id AS CorrelationId,
            j.cancel_requested AS CancelRequested,
            j.cancel_requested_at_utc AS CancelRequestedAtUtc,
            j.cancelled_at_utc AS CancelledAtUtc,
            j.retry_of_import_job_id AS RetryOfImportJobId
        FROM dbo.ImportJobs j
        WHERE j.public_id = @PublicId;
        """;

    public const string GetAllImportErrorsByJobId = """
        SELECT
            e.seq AS Seq,
            e.line_number AS LineNumber,
            e.error_code AS ErrorCode,
            e.message AS Message,
            e.raw_line AS RawLine,
            e.action AS Action,
            e.document AS Document,
            e.email AS Email,
            e.created_at_utc AS CreatedAtUtc
        FROM dbo.ImportJobErrors e
        WHERE e.import_job_id = @ImportJobId
        ORDER BY e.seq ASC;
        """;
}


