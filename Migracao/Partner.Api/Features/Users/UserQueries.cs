namespace Partner.Api.Features.Users;

public static class UserQueries
{
    public const string ListPagedBase = """
        SELECT
            u.id AS Id,
            u.nome AS Name,
            u.login AS Login,
            u.email AS Email,
            CASE WHEN u.admin = 'S' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsAdmin,
            CASE WHEN u.ativo = 'S' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsActive,
            CASE WHEN u.acesso_token = 'S' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS AccessToken
        FROM tb_usuario u
        WHERE 1 = 1
        """;

    public const string CountBase = """
        SELECT COUNT(1)
        FROM tb_usuario u
        WHERE 1 = 1
        """;

    public const string GetById = """
        SELECT
            u.id AS Id,
            u.nome AS Name,
            u.login AS Login,
            u.email AS Email,
            CASE WHEN u.admin = 'S' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsAdmin,
            CASE WHEN u.ativo = 'S' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsActive,
            CASE WHEN u.acesso_token = 'S' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS AccessToken,
            CASE WHEN u.primeiro_acesso = 'S' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS FirstAccess
        FROM tb_usuario u
        WHERE u.id = @Id;
        """;

    public const string GetCompanyIdsByUserIds = """
        SELECT eu.id_usuario AS UserId, eu.id_empresa AS Id
        FROM tb_empresa_usuario eu
        WHERE eu.id_usuario IN @UserIds;
        """;

    public const string GetPermissionCodesByUserIds = """
        SELECT fu.id_usuario AS UserId, f.cod_funcao AS Code
        FROM tb_funcao_usuario fu
        INNER JOIN tb_funcao f ON f.id = fu.id_funcao
        WHERE fu.id_usuario IN @UserIds
          AND f.ativo = 'S';
        """;

    public const string GetCompanyIdsByUserId = """
        SELECT eu.id_empresa
        FROM tb_empresa_usuario eu
        WHERE eu.id_usuario = @UserId;
        """;

    public const string GetFunctionIdsByUserId = """
        SELECT fu.id_funcao
        FROM tb_funcao_usuario fu
        WHERE fu.id_usuario = @UserId;
        """;

    public const string ExistsLogin = """
        SELECT COUNT(1)
        FROM tb_usuario u
        WHERE u.login = @Login
          AND u.ativo = 'S'
          AND (@UserId IS NULL OR u.id <> @UserId);
        """;

    public const string ExistsEmail = """
        SELECT COUNT(1)
        FROM tb_usuario u
        WHERE u.email = @Email
          AND u.ativo = 'S'
          AND (@UserId IS NULL OR u.id <> @UserId);
        """;

    public const string CountActiveCompanies = """
        SELECT COUNT(1)
        FROM tb_empresa e
        WHERE e.ativo = 'S'
          AND e.id IN @CompanyIds;
        """;

    public const string CountActiveFunctions = """
        SELECT COUNT(1)
        FROM tb_funcao f
        WHERE f.ativo = 'S'
          AND f.id IN @FunctionIds;
        """;

    public const string InsertUser = """
        INSERT INTO tb_usuario
        (
            nome,
            login,
            senha,
            email,
            primeiro_acesso,
            acesso_token,
            admin,
            ativo,
            id_usuario_cadastro
        )
        VALUES
        (
            @Name,
            @Login,
            @PasswordHash,
            @Email,
            @FirstAccess,
            @AccessToken,
            @Admin,
            @Active,
            @CreatedByUserId
        );

        SELECT CAST(SCOPE_IDENTITY() AS int);
        """;

    public const string UpdateUser = """
        UPDATE tb_usuario
        SET
            nome = @Name,
            login = @Login,
            email = @Email,
            acesso_token = @AccessToken,
            admin = @Admin,
            ativo = @Active,
            id_usuario_cadastro = @UpdatedByUserId
        WHERE id = @Id;
        """;

    public const string InactivateUser = """
        UPDATE tb_usuario
        SET
            ativo = 'N',
            id_usuario_cadastro = @UpdatedByUserId
        WHERE id = @Id;
        """;

    public const string DeleteUserCompanies = """
        DELETE FROM tb_empresa_usuario
        WHERE id_usuario = @UserId;
        """;

    public const string DeleteUserFunctions = """
        DELETE FROM tb_funcao_usuario
        WHERE id_usuario = @UserId;
        """;

    public const string InsertUserCompany = """
        INSERT INTO tb_empresa_usuario (id_empresa, id_usuario)
        VALUES (@CompanyId, @UserId);
        """;

    public const string InsertUserFunction = """
        INSERT INTO tb_funcao_usuario (id_funcao, id_usuario)
        VALUES (@FunctionId, @UserId);
        """;

    public const string LookupCompanies = """
        SELECT e.id AS Id, e.nome_fantasia AS Name
        FROM tb_empresa e
        WHERE e.ativo = 'S'
        ORDER BY e.nome_fantasia;
        """;

    public const string LookupFunctions = """
        SELECT f.id AS Id, f.cod_funcao AS Code, f.nome_funcao AS Name
        FROM tb_funcao f
        WHERE f.ativo = 'S'
        ORDER BY f.nome_funcao;
        """;
}

