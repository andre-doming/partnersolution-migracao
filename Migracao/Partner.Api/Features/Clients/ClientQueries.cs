namespace Partner.Api.Features.Clients;

public static class ClientQueries
{
    public const string ListPagedBase = """
        SELECT
            c.id AS Id,
            c.id_cliente AS ClientGuid,
            c.nome AS FirstName,
            c.sobrenome AS LastName,
            c.cpf AS Document,
            c.email AS Email,
            CAST(NULL AS varchar(40)) AS Gender,
            CAST(NULL AS varchar(20)) AS BirthDate,
            e.id AS CompanyId,
            e.nome_fantasia AS CompanyName,
            CAST(NULL AS varchar(120)) AS Department,
            c.cargo AS Role,
            CAST(0 AS bit) AS Approved,
            CASE WHEN c.ativo = 'S' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsActive
        FROM tb_cliente c
        INNER JOIN tb_empresa e ON e.id_parceiro = c.id_parceiro
        WHERE 1 = 1
        """;

    public const string CountBase = """
        SELECT COUNT(1)
        FROM tb_cliente c
        INNER JOIN tb_empresa e ON e.id_parceiro = c.id_parceiro
        WHERE 1 = 1
        """;

    public const string GetById = """
        SELECT
            c.id AS Id,
            c.id_cliente AS ClientGuid,
            c.nome AS FirstName,
            c.sobrenome AS LastName,
            c.cpf AS Document,
            c.email AS Email,
            CAST(NULL AS varchar(40)) AS Gender,
            CAST(NULL AS varchar(20)) AS BirthDate,
            e.id AS CompanyId,
            e.nome_fantasia AS CompanyName,
            CAST(NULL AS varchar(120)) AS Department,
            c.cargo AS Role,
            CAST(0 AS bit) AS Approved,
            CASE WHEN c.ativo = 'S' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsActive
        FROM tb_cliente c
        INNER JOIN tb_empresa e ON e.id_parceiro = c.id_parceiro
        WHERE c.id = @Id;
        """;

    public const string ExistsDocument = """
        SELECT COUNT(1)
        FROM tb_cliente c
        WHERE c.cpf = @Document
          AND c.id_parceiro = @PartnerId
          AND c.ativo = 'S'
          AND (@Id IS NULL OR c.id <> @Id);
        """;

    public const string ExistsEmail = """
        SELECT COUNT(1)
        FROM tb_cliente c
        WHERE c.email = @Email
          AND c.id_parceiro = @PartnerId
          AND c.ativo = 'S'
          AND (@Id IS NULL OR c.id <> @Id);
        """;

    public const string CountCompanyById = """
        SELECT COUNT(1)
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

    public const string GetCompanyMapByIds = """
        SELECT
            e.id AS CompanyId,
            e.id_parceiro AS PartnerId,
            e.nome_fantasia AS CompanyName
        FROM tb_empresa e
        WHERE e.id IN @CompanyIds
          AND e.ativo = 'S';
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

    public const string LookupCompanies = """
        SELECT
            e.id AS Id,
            e.nome_fantasia AS Name
        FROM tb_empresa e
        WHERE e.ativo = 'S'
        ORDER BY e.nome_fantasia;
        """;

    public const string Insert = """
        INSERT INTO tb_cliente
        (
            nome,
            sobrenome,
            cpf,
            email,
            empresa,
            id_parceiro,
            cargo,
            ativo,
            id_cliente
        )
        VALUES
        (
            @FirstName,
            @LastName,
            @Document,
            @Email,
            @CompanyName,
            @PartnerId,
            @Role,
            @Active,
            @ClientGuid
        );

        SELECT CAST(SCOPE_IDENTITY() AS int);
        """;

    public const string Update = """
        UPDATE tb_cliente
        SET
            nome = @FirstName,
            sobrenome = @LastName,
            cpf = @Document,
            email = @Email,
            empresa = @CompanyName,
            id_parceiro = @PartnerId,
            cargo = @Role,
            ativo = @Active
        WHERE id = @Id;
        """;

    public const string Inactivate = """
        UPDATE tb_cliente
        SET
            ativo = 'N'
        WHERE id = @Id;
        """;
}

