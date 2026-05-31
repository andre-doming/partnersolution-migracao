using Dapper;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Partner.Api.Infrastructure.Database;
using Partner.Api.Infrastructure.Security;
using Partner.Api.Features.Auth.Mfa;
using Partner.Api.Infrastructure.RateLimiting;
using Partner.Api.Middleware;
using System.Security.Cryptography;

namespace Partner.Api.Features.Auth;

public static class AuthEndpoints
{
    private const string PasswordLockoutMessage = "Conta temporariamente bloqueada. Tente novamente mais tarde.";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .WithMetadata(new RateLimitPolicyMetadata(RateLimitingPolicyNames.AuthLogin))
            .RequireRateLimiting(RateLimitingPolicyNames.AuthLogin)
            .Produces<AuthLoginResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] AuthLoginRequest request,
        IValidator<AuthLoginRequest> validator,
        ISqlConnectionFactory connectionFactory,
        IPasswordHasher passwordHasher,
        IOptions<PasswordLockoutOptions> lockoutOptions,
        IOptions<MfaOptions> mfaOptions,
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> jwtOptions,
        PendingTokenService pendingTokenService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        using var connection = connectionFactory.CreateConnection();
        var user = await connection.QueryFirstOrDefaultAsync<AuthUser>(
            new CommandDefinition(AuthQueries.GetUserByLogin, new { request.Login }, cancellationToken: cancellationToken));

        if (user is null || !user.IsActive)
        {
            return Results.Unauthorized();
        }

        await connection.ExecuteAsync(new CommandDefinition(
            MfaQueries.EnsureMfaRowForUser,
            new { UserId = user.Id },
            cancellationToken: cancellationToken));

        var now = DateTime.UtcNow;
        var lockoutUntil = user.PasswordLockoutUntil;
        var failedAttempts = user.FailedPasswordAttempts;
        if (lockoutUntil is not null && lockoutUntil > now)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                MfaQueries.InsertAuditLog,
                new
                {
                    UserId = user.Id,
                    ActorUserId = user.Id,
                    EventType = "PASSWORD_LOCKOUT",
                    EventDescription = "Login blocked due to password lockout",
                    CorrelationId = CorrelationIdMiddleware.GetCorrelationId(httpContext),
                    IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                    MetadataJson = (string?)null,
                    CreatedAt = now
                },
                cancellationToken: cancellationToken));

            return Results.Json(new { message = PasswordLockoutMessage }, statusCode: StatusCodes.Status401Unauthorized);
        }

        if (lockoutUntil is not null)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                MfaQueries.ResetPasswordLockout,
                new { UserId = user.Id, UpdatedAt = now },
                cancellationToken: cancellationToken));

            failedAttempts = 0;
            lockoutUntil = null;
        }

        var hasValidPassword = passwordHasher.IsModernHash(user.PasswordHash)
            ? passwordHasher.Verify(request.Password, user.PasswordHash)
            : PasswordVerifier.VerifyLegacyMd5(request.Password, user.PasswordHash);

        if (!hasValidPassword)
        {
            var maxAttempts = Math.Max(1, lockoutOptions.Value.PasswordMaxAttempts);
            var nextAttempts = failedAttempts + 1;
            DateTime? nextLockoutUntil = null;

            if (nextAttempts >= maxAttempts)
            {
                nextLockoutUntil = now.AddMinutes(lockoutOptions.Value.PasswordLockoutMinutes);
                nextAttempts = maxAttempts;
            }

            await connection.ExecuteAsync(new CommandDefinition(
                MfaQueries.UpdatePasswordLockout,
                new
                {
                    UserId = user.Id,
                    FailedPasswordAttempts = nextAttempts,
                    PasswordLockoutUntil = nextLockoutUntil,
                    UpdatedAt = now
                },
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                MfaQueries.InsertAuditLog,
                new
                {
                    UserId = user.Id,
                    ActorUserId = user.Id,
                    EventType = "PASSWORD_FAILURE",
                    EventDescription = "Invalid password",
                    CorrelationId = CorrelationIdMiddleware.GetCorrelationId(httpContext),
                    IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                    MetadataJson = (string?)null,
                    CreatedAt = now
                },
                cancellationToken: cancellationToken));

            if (nextLockoutUntil is not null)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    MfaQueries.InsertAuditLog,
                    new
                    {
                        UserId = user.Id,
                        ActorUserId = user.Id,
                        EventType = "PASSWORD_LOCKOUT",
                        EventDescription = "Password lockout applied",
                        CorrelationId = CorrelationIdMiddleware.GetCorrelationId(httpContext),
                        IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                        MetadataJson = (string?)null,
                        CreatedAt = now
                    },
                    cancellationToken: cancellationToken));
            }

            return nextLockoutUntil is null
                ? Results.Unauthorized()
                : Results.Json(new { message = PasswordLockoutMessage }, statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!passwordHasher.IsModernHash(user.PasswordHash))
        {
            var modernHash = passwordHasher.Hash(request.Password);

            try
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    AuthQueries.UpdateUserPasswordHashById,
                    new { UserId = user.Id, PasswordHash = modernHash },
                    cancellationToken: cancellationToken));
            }
            catch (SqlException exception) when (exception.Number == 2628)
            {
                // Legacy schema may have a short senha column.
                // Keep login successful and postpone hash migration until column size is expanded.
            }
        }

        if (failedAttempts > 0 || lockoutUntil is not null)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                MfaQueries.ResetPasswordLockout,
                new { UserId = user.Id, UpdatedAt = now },
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                MfaQueries.InsertAuditLog,
                new
                {
                    UserId = user.Id,
                    ActorUserId = user.Id,
                    EventType = "PASSWORD_SUCCESS_AFTER_LOCKOUT",
                    EventDescription = "Password validated after failures",
                    CorrelationId = CorrelationIdMiddleware.GetCorrelationId(httpContext),
                    IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                    MetadataJson = (string?)null,
                    CreatedAt = now
                },
                cancellationToken: cancellationToken));
        }

        await connection.ExecuteAsync(new CommandDefinition(
            MfaQueries.InsertAuditLog,
            new
            {
                UserId = user.Id,
                ActorUserId = user.Id,
                EventType = "LOGIN_PASSWORD_OK",
                EventDescription = "Password validated",
                CorrelationId = CorrelationIdMiddleware.GetCorrelationId(httpContext),
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                MetadataJson = (string?)null,
                CreatedAt = now
            },
            cancellationToken: cancellationToken));

        var mfaState = await connection.QueryFirstOrDefaultAsync<UserMfaState>(
            new CommandDefinition(MfaQueries.GetMfaStateByUserId, new { UserId = user.Id }, cancellationToken: cancellationToken));

        if (mfaState is null)
        {
            return Results.Unauthorized();
        }

        if (!mfaState.MfaEnabled || mfaState.MfaResetRequired)
        {
            var setupPermissions = (await connection.QueryAsync<string>(
                new CommandDefinition(AuthQueries.GetPermissionsByUserId, new { UserId = user.Id }, cancellationToken: cancellationToken)))
                .ToArray();

            var setupCompanies = (await connection.QueryAsync<int>(
                new CommandDefinition(AuthQueries.GetCompanyIdsByUserId, new { UserId = user.Id }, cancellationToken: cancellationToken)))
                .ToArray();

            var setupToken = jwtTokenService.GenerateToken(user, setupPermissions, setupCompanies);
            var setupExpiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpirationMinutes);

            await connection.ExecuteAsync(new CommandDefinition(
                MfaQueries.InsertAuditLog,
                new
                {
                    UserId = user.Id,
                    ActorUserId = user.Id,
                    EventType = "MFA_SETUP_REQUIRED",
                    EventDescription = "MFA setup required",
                    CorrelationId = CorrelationIdMiddleware.GetCorrelationId(httpContext),
                    IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                    MetadataJson = (string?)null,
                    CreatedAt = DateTime.UtcNow
                },
                cancellationToken: cancellationToken));

            return Results.Ok(new AuthLoginResponse
            {
                Status = MfaRules.StatusMfaSetupRequired,
                AccessToken = setupToken,
                ExpiresAtUtc = setupExpiresAt,
                Name = user.Name
            });
        }

        var pendingId = Guid.NewGuid();
        var pendingToken = pendingTokenService.GenerateToken();
        var tokenHash = pendingTokenService.HashToken(pendingToken);
        var expiresAt = DateTime.UtcNow.AddMinutes(mfaOptions.Value.MfaPendingTokenMinutes);

        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        var userAgentHash = string.IsNullOrWhiteSpace(userAgent)
            ? null
            : SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(userAgent));

        await connection.ExecuteAsync(new CommandDefinition(
            MfaQueries.InsertPendingSession,
            new
            {
                Id = pendingId,
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgentHash = userAgentHash,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            MfaQueries.InsertAuditLog,
            new
            {
                UserId = user.Id,
                ActorUserId = user.Id,
                EventType = "MFA_REQUIRED",
                EventDescription = "MFA verification required",
                CorrelationId = CorrelationIdMiddleware.GetCorrelationId(httpContext),
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = userAgent,
                MetadataJson = (string?)null,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken: cancellationToken));

        return Results.Ok(new AuthLoginResponse
        {
            Status = MfaRules.StatusMfaRequired,
            PendingToken = $"{pendingId}.{pendingToken}"
        });
    }
}
