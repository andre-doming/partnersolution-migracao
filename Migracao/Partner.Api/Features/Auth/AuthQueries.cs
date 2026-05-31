namespace Partner.Api.Features.Auth;

public static class AuthQueries
{
    public const string GetUserByLogin = """
        SELECT
            u.id             AS Id,
            u.nome           AS Name,
            u.login          AS Login,
            u.senha          AS PasswordHash,
            CASE WHEN u.admin = 'S' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsAdmin,
            CASE WHEN u.ativo = 'S' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsActive,
            m.FailedPasswordAttempts AS FailedPasswordAttempts,
            m.PasswordLockoutUntil AS PasswordLockoutUntil
        FROM tb_usuario u
        LEFT JOIN dbo.tb_usuario_mfa m ON m.UserId = u.id
        WHERE u.login = @Login;
        """;

    public const string GetUserById = """
        SELECT
            u.id             AS Id,
            u.nome           AS Name,
            u.login          AS Login,
            u.senha          AS PasswordHash,
            CASE WHEN u.admin = 'S' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsAdmin,
            CASE WHEN u.ativo = 'S' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsActive,
            m.FailedPasswordAttempts AS FailedPasswordAttempts,
            m.PasswordLockoutUntil AS PasswordLockoutUntil
        FROM tb_usuario u
        LEFT JOIN dbo.tb_usuario_mfa m ON m.UserId = u.id
        WHERE u.id = @UserId;
        """;

    public const string GetPermissionsByUserId = """
        SELECT f.cod_funcao
        FROM tb_funcao_usuario fu
        INNER JOIN tb_funcao f ON f.id = fu.id_funcao
        WHERE fu.id_usuario = @UserId
          AND f.ativo = 'S';
        """;

    public const string GetCompanyIdsByUserId = """
        SELECT eu.id_empresa
        FROM tb_empresa_usuario eu
        WHERE eu.id_usuario = @UserId;
        """;

    public const string UpdateUserPasswordHashById = """
        UPDATE tb_usuario
        SET senha = @PasswordHash
        WHERE id = @UserId;
        """;
}
