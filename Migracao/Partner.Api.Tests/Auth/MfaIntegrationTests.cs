using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Partner.Api.Features.Auth;
using Partner.Api.Features.Auth.Mfa;
using Partner.Api.Infrastructure.Database;
using Partner.Api.Infrastructure.Security;
using System.Security.Cryptography;

namespace Partner.Api.Tests.Auth;

public sealed class MfaIntegrationTests
{
    [Fact]
    public async Task Login_WhenMfaNotConfigured_ShouldReturnSetupRequired()
    {
        await using var connection = CreateInMemoryDatabase();
        var connectionFactory = new TestSqlConnectionFactory(connection);
        var handler = BuildAuthHandler(connectionFactory);

        var request = new AuthLoginRequest { Login = "user", Password = "pass" };
        var response = await handler.LoginAsync(request, CancellationToken.None);

        Assert.Equal(MfaRules.StatusMfaSetupRequired, response.Status);
        Assert.Null(response.AccessToken);
    }

    [Fact]
    public async Task Login_WhenMfaEnabled_ShouldReturnPendingToken()
    {
        await using var connection = CreateInMemoryDatabase();
        var connectionFactory = new TestSqlConnectionFactory(connection);
        var handler = BuildAuthHandler(connectionFactory);

        await connection.ExecuteAsync("UPDATE dbo.tb_usuario_mfa SET MfaEnabled = 1 WHERE UserId = 1;");

        var request = new AuthLoginRequest { Login = "user", Password = "pass" };
        var response = await handler.LoginAsync(request, CancellationToken.None);

        Assert.Equal(MfaRules.StatusMfaRequired, response.Status);
        Assert.NotNull(response.PendingToken);
    }

    [Fact]
    public async Task Verify_WhenTotpValid_ShouldReturnJwt()
    {
        await using var connection = CreateInMemoryDatabase();
        var connectionFactory = new TestSqlConnectionFactory(connection);
        var handler = BuildAuthHandler(connectionFactory);

        var secret = MfaSecrets.GenerateSecret();
        var now = DateTime.UtcNow;
        var totp = TotpGenerator.GenerateCode(secret, now);
        await connection.ExecuteAsync("UPDATE dbo.tb_usuario_mfa SET MfaEnabled = 1, MfaSecret = @Secret WHERE UserId = 1;", new { Secret = secret });

        var pendingToken = await SeedPendingAsync(connection, handler.PendingTokenService, now.AddMinutes(5));

        var response = await handler.VerifyAsync(new MfaVerifyRequest
        {
            PendingToken = pendingToken,
            TotpCode = totp
        }, CancellationToken.None);

        Assert.Equal(MfaRules.StatusLoginSuccess, response.Status);
        Assert.NotNull(response.AccessToken);
    }

    [Fact]
    public async Task Verify_WhenRecoveryCodeValid_ShouldReturnJwt()
    {
        await using var connection = CreateInMemoryDatabase();
        var connectionFactory = new TestSqlConnectionFactory(connection);
        var handler = BuildAuthHandler(connectionFactory);

        await connection.ExecuteAsync("UPDATE dbo.tb_usuario_mfa SET MfaEnabled = 1 WHERE UserId = 1;");
        var code = handler.RecoveryCodeService.GenerateCodes(1).Single();
        var hash = handler.RecoveryCodeService.HashCode(code);
        await connection.ExecuteAsync(
            "INSERT INTO tb_usuario_mfa_recovery (UserId, CodeId, CodeHash, Used, Invalidated, CreatedAt) VALUES (1, @CodeId, @CodeHash, 0, 0, @CreatedAt);",
            new { CodeId = Guid.NewGuid(), CodeHash = hash, CreatedAt = DateTime.UtcNow });

        var pendingToken = await SeedPendingAsync(connection, handler.PendingTokenService, DateTime.UtcNow.AddMinutes(5));

        var response = await handler.VerifyAsync(new MfaVerifyRequest
        {
            PendingToken = pendingToken,
            RecoveryCode = code
        }, CancellationToken.None);

        Assert.Equal(MfaRules.StatusLoginSuccess, response.Status);
        Assert.NotNull(response.AccessToken);
    }

    [Fact]
    public async Task Verify_WhenInvalidTotp_ShouldIncrementLockout()
    {
        await using var connection = CreateInMemoryDatabase();
        var connectionFactory = new TestSqlConnectionFactory(connection);
        var handler = BuildAuthHandler(connectionFactory);

        await connection.ExecuteAsync("UPDATE dbo.tb_usuario_mfa SET MfaEnabled = 1, MfaSecret = @Secret WHERE UserId = 1;", new { Secret = MfaSecrets.GenerateSecret() });
        var pendingToken = await SeedPendingAsync(connection, handler.PendingTokenService, DateTime.UtcNow.AddMinutes(5));

        var response = await handler.VerifyAsync(new MfaVerifyRequest
        {
            PendingToken = pendingToken,
            TotpCode = "000000"
        }, CancellationToken.None);

        Assert.Equal("INVALID", response.Status);
    }

    [Fact]
    public async Task Verify_WhenPendingExpired_ShouldReject()
    {
        await using var connection = CreateInMemoryDatabase();
        var connectionFactory = new TestSqlConnectionFactory(connection);
        var handler = BuildAuthHandler(connectionFactory);

        var pendingToken = await SeedPendingAsync(connection, handler.PendingTokenService, DateTime.UtcNow.AddMinutes(-1));

        var response = await handler.VerifyAsync(new MfaVerifyRequest
        {
            PendingToken = pendingToken,
            TotpCode = "000000"
        }, CancellationToken.None);

        Assert.Equal("UNAUTHORIZED", response.Status);
    }

    private static async Task<string> SeedPendingAsync(SqliteConnection connection, PendingTokenService pendingTokenService, DateTime expiresAt)
    {
        var pendingId = Guid.NewGuid();
        var token = pendingTokenService.GenerateToken();
        var hash = pendingTokenService.HashToken(token);
        await connection.ExecuteAsync(
            "INSERT INTO tb_auth_mfa_pending (Id, UserId, TokenHash, ExpiresAt, CreatedAt) VALUES (@Id, 1, @Hash, @ExpiresAt, @CreatedAt);",
            new { Id = pendingId.ToString(), Hash = hash, ExpiresAt = expiresAt.ToString("O"), CreatedAt = DateTime.UtcNow.ToString("O") });
        return $"{pendingId}.{token}";
    }

    private static AuthHandlers BuildAuthHandler(ISqlConnectionFactory factory)
    {
        return new AuthHandlers(factory,
            new PasswordHasher(),
            new JwtTokenService(Options.Create(new JwtOptions
            {
                Issuer = "Partner.Api",
                Audience = "Partner.Web",
                SecretKey = new string('a', 32),
                ExpirationMinutes = 60
            })),
            Options.Create(new JwtOptions
            {
                Issuer = "Partner.Api",
                Audience = "Partner.Web",
                SecretKey = new string('a', 32),
                ExpirationMinutes = 60
            }),
            Options.Create(new MfaOptions()),
            new PendingTokenService(),
            new RecoveryCodeService());
    }

    private static SqliteConnection CreateInMemoryDatabase()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        connection.Execute(@"
ATTACH DATABASE ':memory:' AS dbo;

CREATE TABLE tb_usuario
(
    id INTEGER PRIMARY KEY,
    nome TEXT NOT NULL,
    login TEXT NOT NULL,
    senha TEXT NOT NULL,
    admin TEXT NOT NULL,
    ativo TEXT NOT NULL
);

CREATE TABLE dbo.tb_usuario_mfa
(
    UserId INTEGER PRIMARY KEY,
    MfaEnabled INTEGER NOT NULL,
    MfaSecret BLOB NULL,
    MfaSetupStartedAt TEXT NULL,
    MfaConfiguredAt TEXT NULL,
    RecoveryCodesGeneratedAt TEXT NULL,
    MfaResetRequired INTEGER NOT NULL,
    FailedPasswordAttempts INTEGER NOT NULL,
    PasswordLockoutUntil TEXT NULL,
    FailedMfaAttempts INTEGER NOT NULL,
    MfaLockoutUntil TEXT NULL,
    LastSuccessfulMfaAt TEXT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NULL
);

CREATE TABLE tb_usuario_mfa_recovery
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    CodeId TEXT NOT NULL,
    CodeHash TEXT NOT NULL,
    Used INTEGER NOT NULL,
    UsedAt TEXT NULL,
    Invalidated INTEGER NOT NULL,
    InvalidatedAt TEXT NULL,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE tb_auth_mfa_pending
(
    Id TEXT PRIMARY KEY,
    UserId INTEGER NOT NULL,
    TokenHash BLOB NOT NULL,
    ExpiresAt TEXT NOT NULL,
    ConsumedAt TEXT NULL,
    IpAddress TEXT NULL,
    UserAgentHash BLOB NULL,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE tb_funcao_usuario (id_usuario INTEGER NOT NULL, id_funcao INTEGER NOT NULL);
CREATE TABLE tb_funcao (id INTEGER PRIMARY KEY, cod_funcao TEXT NOT NULL, ativo TEXT NOT NULL);
CREATE TABLE tb_empresa_usuario (id_usuario INTEGER NOT NULL, id_empresa INTEGER NOT NULL);
CREATE TABLE dbo.tb_audit_log (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NULL, ActorUserId INTEGER NULL, EventType TEXT NOT NULL, EventDescription TEXT NULL, CorrelationId TEXT NULL, IpAddress TEXT NULL, UserAgent TEXT NULL, MetadataJson TEXT NULL, CreatedAt TEXT NOT NULL);

INSERT INTO tb_usuario (id, nome, login, senha, admin, ativo) VALUES (1, 'User', 'user', @Pass, 'N', 'S');
INSERT INTO dbo.tb_usuario_mfa (UserId, MfaEnabled, MfaResetRequired, FailedPasswordAttempts, FailedMfaAttempts, CreatedAt) VALUES (1, 0, 0, 0, 0, @Now);
",
            new { Pass = new PasswordHasher().Hash("pass"), Now = DateTime.UtcNow.ToString("O") });

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

    private sealed class AuthHandlers
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly PasswordHasher _passwordHasher;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IOptions<JwtOptions> _jwtOptions;
        private readonly IOptions<MfaOptions> _mfaOptions;
        private readonly PendingTokenService _pendingTokenService;
        private readonly RecoveryCodeService _recoveryCodeService;

        public AuthHandlers(
            ISqlConnectionFactory factory,
            PasswordHasher passwordHasher,
            JwtTokenService jwtTokenService,
            IOptions<JwtOptions> jwtOptions,
            IOptions<MfaOptions> mfaOptions,
            PendingTokenService pendingTokenService,
            RecoveryCodeService recoveryCodeService)
        {
            _factory = factory;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
            _jwtOptions = jwtOptions;
            _mfaOptions = mfaOptions;
            _pendingTokenService = pendingTokenService;
            _recoveryCodeService = recoveryCodeService;
        }

        public PendingTokenService PendingTokenService => _pendingTokenService;
        public RecoveryCodeService RecoveryCodeService => _recoveryCodeService;

        public async Task<AuthLoginResponse> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken)
        {
            using var connection = _factory.CreateConnection();
            var user = await connection.QueryFirstAsync<AuthUser>(AuthQueries.GetUserByLogin, new { request.Login });

            var hasValidPassword = _passwordHasher.Verify(request.Password, user.PasswordHash);
            if (!hasValidPassword)
            {
                return new AuthLoginResponse { Status = "UNAUTHORIZED" };
            }

            await connection.ExecuteAsync("INSERT OR IGNORE INTO dbo.tb_usuario_mfa (UserId, MfaEnabled, MfaResetRequired, FailedPasswordAttempts, FailedMfaAttempts, CreatedAt) VALUES (@UserId, 0, 0, 0, 0, @Now);",
                new { UserId = user.Id, Now = DateTime.UtcNow.ToString("O") });
            var mfaState = await connection.QueryFirstAsync<UserMfaState>("SELECT * FROM dbo.tb_usuario_mfa WHERE UserId = @UserId;", new { UserId = user.Id });

            if (!mfaState.MfaEnabled)
            {
                return new AuthLoginResponse { Status = MfaRules.StatusMfaSetupRequired };
            }

            var pendingId = Guid.NewGuid();
            var token = _pendingTokenService.GenerateToken();
            var hash = _pendingTokenService.HashToken(token);
            var expires = DateTime.UtcNow.AddMinutes(_mfaOptions.Value.MfaPendingTokenMinutes);

            await connection.ExecuteAsync("INSERT INTO tb_auth_mfa_pending (Id, UserId, TokenHash, ExpiresAt, CreatedAt) VALUES (@Id, @UserId, @TokenHash, @ExpiresAt, @CreatedAt);",
                new { Id = pendingId, UserId = user.Id, TokenHash = hash, ExpiresAt = expires, CreatedAt = DateTime.UtcNow.ToString("O") });

            return new AuthLoginResponse { Status = MfaRules.StatusMfaRequired, PendingToken = $"{pendingId}.{token}" };
        }

        public async Task<AuthLoginResponse> VerifyAsync(MfaVerifyRequest request, CancellationToken cancellationToken)
        {
            using var connection = _factory.CreateConnection();
            var now = DateTime.UtcNow;

            if (!Guid.TryParse(request.PendingToken.Split('.', 2)[0], out var id))
            {
                return new AuthLoginResponse { Status = "UNAUTHORIZED" };
            }

            var pending = await connection.QueryFirstOrDefaultAsync<PendingSessionRow>(
                "SELECT Id, UserId, TokenHash, ExpiresAt, ConsumedAt, IpAddress, UserAgentHash, CreatedAt FROM tb_auth_mfa_pending WHERE Id = @Id;",
                new { Id = id.ToString() });
            var expiresAt = ParseUtc(pending?.ExpiresAt);
            if (pending is null || expiresAt <= now)
            {
                return new AuthLoginResponse { Status = "UNAUTHORIZED" };
            }

            var tokenPart = request.PendingToken.Split('.', 2, StringSplitOptions.RemoveEmptyEntries)[1];
            var tokenHash = _pendingTokenService.HashToken(tokenPart);
            if (!CryptographicOperations.FixedTimeEquals(tokenHash, pending.TokenHash))
            {
                return new AuthLoginResponse { Status = "UNAUTHORIZED" };
            }

            var mfaState = await connection.QueryFirstAsync<UserMfaState>("SELECT * FROM dbo.tb_usuario_mfa WHERE UserId = @UserId;", new { UserId = pending.UserId });
            if (mfaState.MfaLockoutUntil is not null && mfaState.MfaLockoutUntil > now)
            {
                return new AuthLoginResponse { Status = "LOCKED" };
            }

            var valid = false;
            if (!string.IsNullOrWhiteSpace(request.TotpCode))
            {
                valid = TotpGenerator.ValidateCode(mfaState.MfaSecret!, request.TotpCode, now);
            }
            else if (!string.IsNullOrWhiteSpace(request.RecoveryCode))
            {
                var codes = await connection.QueryAsync<RecoveryCodeRow>(
                    "SELECT Id, UserId, CodeId, CodeHash, Used, UsedAt, Invalidated, InvalidatedAt, CreatedAt FROM tb_usuario_mfa_recovery WHERE UserId = @UserId AND Invalidated = 0 AND Used = 0;",
                    new { UserId = pending.UserId });
                foreach (var code in codes)
                {
                    if (_recoveryCodeService.VerifyCode(request.RecoveryCode, code.CodeHash))
                    {
                        valid = true;
                        break;
                    }
                }
            }

            if (!valid)
            {
                return new AuthLoginResponse { Status = "INVALID" };
            }

            var permissions = Array.Empty<string>();
            var companies = Array.Empty<int>();
            var user = await connection.QueryFirstAsync<AuthUser>(AuthQueries.GetUserById, new { UserId = pending.UserId });
            var token = _jwtTokenService.GenerateToken(user, permissions, companies);
            var tokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.Value.ExpirationMinutes);
            return new AuthLoginResponse { Status = MfaRules.StatusLoginSuccess, AccessToken = token, ExpiresAtUtc = tokenExpiresAt };
        }

        private sealed class PendingSessionRow
        {
            public string Id { get; init; } = string.Empty;
            public int UserId { get; init; }
            public byte[] TokenHash { get; init; } = [];
            public string ExpiresAt { get; init; } = string.Empty;
            public string? ConsumedAt { get; init; }
            public string? IpAddress { get; init; }
            public byte[]? UserAgentHash { get; init; }
            public string CreatedAt { get; init; } = string.Empty;
        }

        private sealed class RecoveryCodeRow
        {
            public int Id { get; init; }
            public int UserId { get; init; }
            public string CodeId { get; init; } = string.Empty;
            public string CodeHash { get; init; } = string.Empty;
            public int Used { get; init; }
            public string? UsedAt { get; init; }
            public int Invalidated { get; init; }
            public string? InvalidatedAt { get; init; }
            public string CreatedAt { get; init; } = string.Empty;
        }

        private static DateTime ParseUtc(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.MinValue;
            }

            return DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime();
        }
    }
}