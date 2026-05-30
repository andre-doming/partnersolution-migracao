namespace Partner.Api.Features.Companies;

public static class CompanyQueries
{
    public const string ListPagedBase = """
        SELECT
            e.id AS Id,
            e.cnpj AS Cnpj,
            e.nome_fantasia AS TradeName,
            e.razao_social AS CorporateName,
            e.gerente_responsavel AS ManagerName,
            CASE WHEN e.ativo = 'S' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsActive
        FROM tb_empresa e
        WHERE 1 = 1
        """;

    public const string CountBase = """
        SELECT COUNT(1)
        FROM tb_empresa e
        WHERE 1 = 1
        """;

    public const string GetById = """
        SELECT
            e.id AS Id,
            e.cnpj AS Cnpj,
            e.nome_fantasia AS TradeName,
            e.razao_social AS CorporateName,
            e.gerente_responsavel AS ManagerName,
            CASE WHEN e.ativo = 'S' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsActive
        FROM tb_empresa e
        WHERE e.id = @Id;
        """;

    public const string ExistsCnpj = """
        SELECT COUNT(1)
        FROM tb_empresa e
        WHERE e.cnpj = @Cnpj
          AND e.ativo = 'S'
          AND (@Id IS NULL OR e.id <> @Id);
        """;

    public const string Insert = """
        INSERT INTO tb_empresa
        (
            cnpj,
            nome_fantasia,
            razao_social,
            gerente_responsavel,
            ativo,
            id_usuario_cadastro
        )
        VALUES
        (
            @Cnpj,
            @TradeName,
            @CorporateName,
            @ManagerName,
            @Active,
            @ActorUserId
        );

        SELECT CAST(SCOPE_IDENTITY() AS int);
        """;

    public const string Update = """
        UPDATE tb_empresa
        SET
            cnpj = @Cnpj,
            nome_fantasia = @TradeName,
            razao_social = @CorporateName,
            gerente_responsavel = @ManagerName,
            ativo = @Active,
            id_usuario_cadastro = @ActorUserId
        WHERE id = @Id;
        """;

    public const string Inactivate = """
        UPDATE tb_empresa
        SET
            ativo = 'N',
            id_usuario_cadastro = @ActorUserId
        WHERE id = @Id;
        """;
}

