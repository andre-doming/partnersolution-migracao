namespace Partner.Api.Features.Import;

public static class ImportQueries
{
    public const string EnsureImportTables = """
        IF OBJECT_ID('dbo.ImportJobs', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ImportJobs
            (
                id INT IDENTITY(1,1) PRIMARY KEY,
                feature NVARCHAR(60) NOT NULL,
                file_name NVARCHAR(260) NOT NULL,
                company_id INT NOT NULL,
                status NVARCHAR(30) NOT NULL,
                total_rows INT NOT NULL,
                success_rows INT NOT NULL,
                error_rows INT NOT NULL,
                duration_ms INT NOT NULL,
                started_at_utc DATETIME2 NOT NULL,
                finished_at_utc DATETIME2 NULL,
                created_by_user_id INT NOT NULL
            );
        END;

        IF OBJECT_ID('dbo.ImportJobErrors', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ImportJobErrors
            (
                id INT IDENTITY(1,1) PRIMARY KEY,
                import_job_id INT NOT NULL,
                line_number INT NOT NULL,
                action NVARCHAR(30) NULL,
                document NVARCHAR(30) NULL,
                email NVARCHAR(150) NULL,
                message NVARCHAR(500) NOT NULL,
                CONSTRAINT FK_ImportJobErrors_ImportJobs FOREIGN KEY (import_job_id) REFERENCES dbo.ImportJobs(id)
            );
        END;
        """;

    public const string InsertImportJob = """
        INSERT INTO dbo.ImportJobs
        (
            feature,
            file_name,
            company_id,
            status,
            total_rows,
            success_rows,
            error_rows,
            duration_ms,
            started_at_utc,
            finished_at_utc,
            created_by_user_id
        )
        VALUES
        (
            @Feature,
            @FileName,
            @CompanyId,
            @Status,
            @TotalRows,
            @SuccessRows,
            @ErrorRows,
            @DurationMs,
            @StartedAtUtc,
            @FinishedAtUtc,
            @CreatedByUserId
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
        ORDER BY j.id DESC
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;
        """;

    public const string CountImportJobs = """
        SELECT COUNT(1)
        FROM dbo.ImportJobs;
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
}

