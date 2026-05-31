using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Partner.Api.Features.Auth;
using Partner.Api.Features.Auth.Mfa;
using Partner.Api.Infrastructure.Database;
using Partner.Api.Infrastructure.Security;

namespace Partner.Api.Tests.Auth;

public sealed class PasswordLockoutIntegrationTests
{
    [Fact]
    public async Task Login_InvalidPassword_ShouldIncrementFailedAttempts()
    {
        await using var connection = CreateInMemoryDatabase();
        var handler = BuildAuthHandler(connection);

        var response = await handler.LoginAsync(new AuthLoginRequest { Login = "user", Password = "wrong" }, CancellationToken.None);

        Assert.Equal("UNAUTHORIZED", response.Status);
        var attempts = await connection.ExecuteScalarAsync<int>("SELECT FailedPasswordAttempts FROM dbo.tb_usuario_mfa WHERE UserId = 1;");
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task Login_WhenMaxAttemptsReached_ShouldLockout()
    {
        await using var connection = CreateInMemoryDatabase();
        var handler = BuildAuthHandler(connection);

        await handler.LoginAsync(new AuthLoginRequest { Login = "user", Password = "wrong" }, CancellationToken.None);
        await handler.LoginAsync(new AuthLoginRequest { Login = "user", Password = "wrong" }, CancellationToken.None);
        await handler.LoginAsync(new AuthLoginRequest { Login = "user", Password = "wrong" }, CancellationToken.None);
        await handler.LoginAsync(new AuthLoginRequest { Login = "user", Password = "wrong" }, CancellationToken.None);
        var response = await handler.LoginAsync(new AuthLoginRequest { Login = "user", Password = "wrong" }, CancellationToken.None);

        Assert.Equal("LOCKED", response.Status);
        var lockoutUntil = await connection.ExecuteScalarAsync<string?>("SELECT PasswordLockoutUntil FROM dbo.tb_usuario_mfa WHERE UserId = 1;");
        Assert.False(string.IsNullOrWhiteSpace(lockoutUntil));
    }

    [Fact]
    public async Task Login_AfterLockoutExpires_ShouldAllowRetry()
    {
        await using var connection = CreateInMemoryDatabase();
        var handler = BuildAuthHandler(connection);

        await connection.ExecuteAsync("UPDATE dbo.tb_usuario_mfa SET FailedPasswordAttempts = 5, PasswordLockoutUntil = @Until WHERE UserId = 1;",
            new { Until = DateTime.UtcNow.AddMinutes(-1).ToString("O") });

        var response = await handler.LoginAsync(new AuthLoginRequest { Login = "user", Password = "wrong" }, CancellationToken.None);

        Assert.Equal("UNAUTHORIZED", response.Status);
        var attempts = await connection.ExecuteScalarAsync<int>("SELECT FailedPasswordAttempts FROM dbo.tb_usuario_mfa WHERE UserId = 1;");
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task Login_WhenPasswordValid_ShouldResetCounters()
    {
        await using var connection = CreateInMemoryDatabase();
        var handler = BuildAuthHandler(connection);

        await connection.ExecuteAsync("UPDATE dbo.tb_usuario_mfa SET FailedPasswordAttempts = 3, PasswordLockoutUntil = NULL WHERE UserId = 1;");

        var response = await handler.LoginAsync(new AuthLoginRequest { Login = "user", Password = "pass" }, CancellationToken.None);

        Assert.Equal(MfaRules.StatusMfaSetupRequired, response.Status);
        var attempts = await connection.ExecuteScalarAsync<int>("SELECT FailedPasswordAttempts FROM dbo.tb_usuario_mfa WHERE UserId = 1;");
        var lockout = await connection.ExecuteScalarAsync<string?>("SELECT PasswordLockoutUntil FROM dbo.tb_usuario_mfa WHERE UserId = 1;");
        Assert.Equal(0, attempts);
        Assert.True(string.IsNullOrWhiteSpace(lockout));
    }

    private static AuthLoginHandler BuildAuthHandler(SqliteConnection connection)
    {
        return new AuthLoginHandler(new TestSqlConnectionFactory(connection),
            new PasswordHasher(),
            Options.Create(new PasswordLockoutOptions { PasswordMaxAttempts = 5, PasswordLockoutMinutes = 15 }),
            Options.Create(new MfaOptions()));
    }

    private static SqliteConnection CreateInMemoryDatabase()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        connection.Execute("ATTACH DATABASE ':memory:' AS dbo;");

        connection.Execute(@"
CREATE TABLE tb_usuario
(
    id INTEGER PRIMARY KEY,
    nome TEXT NOT NULL,
    login TEXT NOT NULL,
    senha TEXT NOT NULL,
    admin TEXT NOT NULL,
    ativo TEXT NOT NULL
);
");

        connection.Execute(@"
CREATE TABLE dbo.tb_usuario_mfa
(
    UserId INTEGER PRIMARY KEY,
    MfaEnabled INTEGER NOT NULL,
    MfaResetRequired INTEGER NOT NULL,
    FailedPasswordAttempts INTEGER NOT NULL,
    PasswordLockoutUntil TEXT NULL,
    FailedMfaAttempts INTEGER NOT NULL,
    MfaLockoutUntil TEXT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NULL
);
");

        connection.Execute(@"
CREATE TABLE dbo.tb_audit_log
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NULL,
    ActorUserId INTEGER NULL,
    EventType TEXT NOT NULL,
    EventDescription TEXT NULL,
    CorrelationId TEXT NULL,
    IpAddress TEXT NULL,
    UserAgent TEXT NULL,
    MetadataJson TEXT NULL,
    CreatedAt TEXT NOT NULL
);
");

        connection.Execute(
            "INSERT INTO tb_usuario (id, nome, login, senha, admin, ativo) VALUES (1, 'User', 'user', @Pass, 'N', 'S');",
            new { Pass = new PasswordHasher().Hash("pass") });

        connection.Execute(
            "INSERT INTO dbo.tb_usuario_mfa (UserId, MfaEnabled, MfaResetRequired, FailedPasswordAttempts, FailedMfaAttempts, CreatedAt) VALUES (1, 0, 0, 0, 0, @Now);",
            new { Now = DateTime.UtcNow.ToString("O") });

        return connection;
    }

    private sealed class TestSqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly SqliteConnection _connection;

        public TestSqlConnectionFactory(SqliteConnection connection)
        {
            _connection = connection;
        }

        public System.Data.IDbConnection CreateConnection() => _connection;
    }
}

internal sealed class AuthLoginHandler
{
    private readonly ISqlConnectionFactory _factory;
    private readonly PasswordHasher _passwordHasher;
    private readonly IOptions<PasswordLockoutOptions> _lockoutOptions;
    private readonly IOptions<MfaOptions> _mfaOptions;

    public AuthLoginHandler(
        ISqlConnectionFactory factory,
        PasswordHasher passwordHasher,
        IOptions<PasswordLockoutOptions> lockoutOptions,
        IOptions<MfaOptions> mfaOptions)
    {
        _factory = factory;
        _passwordHasher = passwordHasher;
        _lockoutOptions = lockoutOptions;
        _mfaOptions = mfaOptions;
    }

    public async Task<AuthLoginResponse> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken)
    {
        var connection = _factory.CreateConnection();
        var user = await connection.QueryFirstAsync<AuthUser>(AuthQueries.GetUserByLogin, new { request.Login });

        await connection.ExecuteAsync(
            "INSERT OR IGNORE INTO dbo.tb_usuario_mfa (UserId, MfaEnabled, MfaResetRequired, FailedPasswordAttempts, FailedMfaAttempts, CreatedAt) VALUES (@UserId, 0, 0, 0, 0, @CreatedAt);",
            new { UserId = user.Id, CreatedAt = DateTime.UtcNow.ToString("O") });

        var now = DateTime.UtcNow;
        var lockoutUntil = user.PasswordLockoutUntil;
        var failedAttempts = user.FailedPasswordAttempts;
        if (lockoutUntil is not null && lockoutUntil > now)
        {
            return new AuthLoginResponse { Status = "LOCKED" };
        }

        if (lockoutUntil is not null)
        {
            await connection.ExecuteAsync(
                "UPDATE dbo.tb_usuario_mfa SET FailedPasswordAttempts = 0, PasswordLockoutUntil = NULL, UpdatedAt = @UpdatedAt WHERE UserId = @UserId;",
                new { UserId = user.Id, UpdatedAt = now.ToString("O") });
            failedAttempts = 0;
            lockoutUntil = null;
        }

        var hasValidPassword = _passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!hasValidPassword)
        {
            var maxAttempts = Math.Max(1, _lockoutOptions.Value.PasswordMaxAttempts);
            var nextAttempts = failedAttempts + 1;
            DateTime? nextLockoutUntil = null;
            if (nextAttempts >= maxAttempts)
            {
                nextLockoutUntil = now.AddMinutes(_lockoutOptions.Value.PasswordLockoutMinutes);
                nextAttempts = maxAttempts;
            }

            await connection.ExecuteAsync(
                "UPDATE dbo.tb_usuario_mfa SET FailedPasswordAttempts = @FailedPasswordAttempts, PasswordLockoutUntil = @PasswordLockoutUntil, UpdatedAt = @UpdatedAt WHERE UserId = @UserId;",
                new
                {
                    UserId = user.Id,
                    FailedPasswordAttempts = nextAttempts,
                    PasswordLockoutUntil = nextLockoutUntil?.ToString("O"),
                    UpdatedAt = now.ToString("O")
                });

            return new AuthLoginResponse { Status = nextLockoutUntil is null ? "UNAUTHORIZED" : "LOCKED" };
        }

        if (failedAttempts > 0 || lockoutUntil is not null)
        {
            await connection.ExecuteAsync(
                "UPDATE dbo.tb_usuario_mfa SET FailedPasswordAttempts = 0, PasswordLockoutUntil = NULL, UpdatedAt = @UpdatedAt WHERE UserId = @UserId;",
                new { UserId = user.Id, UpdatedAt = now.ToString("O") });
        }

        return new AuthLoginResponse { Status = MfaRules.StatusMfaSetupRequired };
    }
}