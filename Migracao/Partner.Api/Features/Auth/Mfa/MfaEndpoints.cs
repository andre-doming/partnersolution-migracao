using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using QRCoder;
using Partner.Api.Features.Auth;
using Partner.Api.Infrastructure.Database;
using Partner.Api.Infrastructure.Security;
using Partner.Api.Middleware;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;

namespace Partner.Api.Features.Auth.Mfa;

public static class MfaEndpoints
{
    public static IEndpointRouteBuilder MapMfaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth/mfa")
            .WithTags("Auth");

        group.MapPost("/setup", SetupAsync)
            .RequireAuthorization()
            .Produces<MfaSetupResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/activate", ActivateAsync)
            .RequireAuthorization()
            .Produces<MfaActivateResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/verify", VerifyAsync)
            .AllowAnonymous()
            .Produces<AuthLoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> SetupAsync(
        ISqlConnectionFactory connectionFactory,
        RecoveryCodeService recoveryCodeService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId(httpContext.User);
        var now = DateTime.UtcNow;

        using var connection = connectionFactory.CreateConnection();

        await connection.ExecuteAsync(new CommandDefinition(
            MfaQueries.EnsureMfaRowForUser,
            new { UserId = userId },
            cancellationToken: cancellationToken));

        var state = await connection.QueryFirstOrDefaultAsync<UserMfaState>(
            new CommandDefinition(MfaQueries.GetMfaStateByUserId, new { UserId = userId }, cancellationToken: cancellationToken));

        if (state is null)
        {
            return Results.BadRequest();
        }

        if (state.MfaEnabled)
        {
            return Results.Conflict(new { message = "MFA already enabled." });
        }

        if (!state.MfaResetRequired && state.MfaConfiguredAt is not null)
        {
            return Results.Conflict(new { message = "MFA already configured." });
        }

        byte[] effectiveSecret;

        if (state.MfaSecret is null || state.MfaSecret.Length == 0)
        {
            var secret = MfaSecrets.GenerateSecret();
            await connection.ExecuteAsync(new CommandDefinition(
                MfaQueries.UpdateMfaSetupStarted,
                new { UserId = userId, MfaSecret = secret, MfaSetupStartedAt = now, UpdatedAt = now },
                cancellationToken: cancellationToken));

            effectiveSecret = secret;
        }
        else
        {
            effectiveSecret = state.MfaSecret;
        }

        var base32 = Base32Encoding.Encode(effectiveSecret);
        var issuer = "Partner";
        var accountName = ResolveUserLogin(httpContext.User);
        var otpAuthUri = OtpAuthUriBuilder.Build(issuer, accountName, base32);

        var qrPng = GenerateQrCodePng(otpAuthUri);
        var qrCodeUrl = $"data:image/png;base64,{Convert.ToBase64String(qrPng)}";

        var maskedSecret = base32.Length <= 4
            ? base32
            : new string('*', base32.Length - 4) + base32[^4..];

        await InsertAuditAsync(connection, new MfaAuditLogEntry
        {
            UserId = userId,
            ActorUserId = userId,
            EventType = "MFA_SETUP_STARTED",
            EventDescription = "MFA setup initiated",
            CorrelationId = CorrelationIdMiddleware.GetCorrelationId(httpContext),
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            MetadataJson = JsonSerializer.Serialize(new { otpAuthUriIssued = true }),
            CreatedAt = now
        }, cancellationToken);

        return Results.Ok(new MfaSetupResponse
        {
            MaskedSecret = maskedSecret,
            ManualEntryKey = base32,
            QrCodeUrl = qrCodeUrl
        });
    }

    private static async Task<IResult> ActivateAsync(
        [FromBody] MfaActivateRequest request,
        ISqlConnectionFactory connectionFactory,
        RecoveryCodeService recoveryCodeService,
        IHostEnvironment environment,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TotpCode))
        {
            return Results.BadRequest(new { message = "TOTP code is required." });
        }

        var userId = ResolveUserId(httpContext.User);
        var now = DateTime.UtcNow;

        using var connection = connectionFactory.CreateConnection();

        var state = await connection.QueryFirstOrDefaultAsync<UserMfaState>(
            new CommandDefinition(MfaQueries.GetMfaStateByUserId, new { UserId = userId }, cancellationToken: cancellationToken));

        if (state is null)
        {
            return Results.BadRequest();
        }

        if (state.MfaEnabled)
        {
            return Results.Conflict(new { message = "MFA already enabled." });
        }

        if (state.MfaSecret is null)
        {
            return Results.Conflict(new { message = "MFA setup not initialized." });
        }

        var isValid = TotpGenerator.ValidateCode(state.MfaSecret, request.TotpCode.Trim(), now);
        if (!isValid)
        {
            object errorPayload;

            if (environment.IsDevelopment())
            {
                errorPayload = new
                {
                    message = "Invalid TOTP code.",
                    serverTimeUtc = now,
                    expectedCodes = Enumerable.Range(-2, 5)
                        .Select(drift => new
                        {
                            drift,
                            code = TotpGenerator.GenerateCode(state.MfaSecret, now.AddSeconds(drift * 30))
                        })
                        .ToArray()
                };
            }
            else
            {
                errorPayload = new { message = "Invalid TOTP code." };
            }

            await InsertAuditAsync(connection, new MfaAuditLogEntry
            {
                UserId = userId,
                ActorUserId = userId,
                EventType = "MFA_FAILED",
                EventDescription = "Invalid TOTP during activation",
                CorrelationId = CorrelationIdMiddleware.GetCorrelationId(httpContext),
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                CreatedAt = now
            }, cancellationToken);

            return Results.BadRequest(errorPayload);
        }

        var recoveryCodes = recoveryCodeService.GenerateCodes();
        foreach (var code in recoveryCodes)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                MfaQueries.InsertRecoveryCode,
                new { UserId = userId, CodeId = Guid.NewGuid(), CodeHash = recoveryCodeService.HashCode(code), CreatedAt = now },
                cancellationToken: cancellationToken));
        }

        await connection.ExecuteAsync(new CommandDefinition(
            MfaQueries.EnableMfa,
            new { UserId = userId, MfaConfiguredAt = now, RecoveryCodesGeneratedAt = now, UpdatedAt = now },
            cancellationToken: cancellationToken));

        await InsertAuditAsync(connection, new MfaAuditLogEntry
        {
            UserId = userId,
            ActorUserId = userId,
            EventType = "MFA_ENABLED",
            EventDescription = "MFA enabled",
            CorrelationId = CorrelationIdMiddleware.GetCorrelationId(httpContext),
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            CreatedAt = now
        }, cancellationToken);

        await InsertAuditAsync(connection, new MfaAuditLogEntry
        {
            UserId = userId,
            ActorUserId = userId,
            EventType = "MFA_RECOVERY_GENERATED",
            EventDescription = "Recovery codes generated",
            CorrelationId = CorrelationIdMiddleware.GetCorrelationId(httpContext),
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            CreatedAt = now
        }, cancellationToken);

        return Results.Ok(new MfaActivateResponse
        {
            RecoveryCodes = recoveryCodes.ToArray()
        });
    }

    private static async Task<IResult> VerifyAsync(
        [FromBody] MfaVerifyRequest request,
        ISqlConnectionFactory connectionFactory,
        PendingTokenService pendingTokenService,
        RecoveryCodeService recoveryCodeService,
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> jwtOptions,
        IOptions<MfaOptions> mfaOptions,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PendingToken))
        {
            return Results.BadRequest(new { message = "pendingToken is required." });
        }

        var now = DateTime.UtcNow;
        using var connection = connectionFactory.CreateConnection();

        if (!TryParsePendingToken(request.PendingToken, out var pendingId, out var tokenPart))
        {
            return Results.BadRequest(new { message = "Invalid pendingToken." });
        }

        var pending = await connection.QueryFirstOrDefaultAsync<MfaPendingSession>(
            new CommandDefinition(MfaQueries.GetPendingSessionById, new { Id = pendingId }, cancellationToken: cancellationToken));

        if (pending is null)
        {
            return Results.Unauthorized();
        }

        if (pending.ConsumedAt is not null || pending.ExpiresAt <= now)
        {
            await connection.ExecuteAsync(new CommandDefinition(MfaQueries.DeletePendingSession, new { Id = pendingId }, cancellationToken: cancellationToken));
            return Results.Unauthorized();
        }

        var tokenHash = pendingTokenService.HashToken(tokenPart);
        if (!CryptographicOperations.FixedTimeEquals(tokenHash, pending.TokenHash))
        {
            return Results.Unauthorized();
        }

        var mfaState = await connection.QueryFirstOrDefaultAsync<UserMfaState>(
            new CommandDefinition(MfaQueries.GetMfaStateByUserId, new { UserId = pending.UserId }, cancellationToken: cancellationToken));

        if (mfaState is null)
        {
            return Results.Unauthorized();
        }

        if (mfaState.MfaLockoutUntil is not null && mfaState.MfaLockoutUntil > now)
        {
            var minutes = (int)Math.Ceiling((mfaState.MfaLockoutUntil.Value - now).TotalMinutes);
            return Results.Conflict(new { message = $"Conta temporariamente bloqueada. Tente novamente após {minutes} minutos." });
        }

        var hasTotp = !string.IsNullOrWhiteSpace(request.TotpCode);
        var hasRecovery = !string.IsNullOrWhiteSpace(request.RecoveryCode);
        if (hasTotp == hasRecovery)
        {
            return Results.BadRequest(new { message = "Provide either totpCode or recoveryCode." });
        }

        var isValid = false;

        if (hasTotp)
        {
            if (mfaState.MfaSecret is null)
            {
                return Results.Unauthorized();
            }

            isValid = TotpGenerator.ValidateCode(mfaState.MfaSecret, request.TotpCode!.Trim(), now);
        }
        else
        {
            var codes = await connection.QueryAsync<UserMfaRecoveryCode>(
                new CommandDefinition(MfaQueries.ListValidRecoveryCodes, new { UserId = pending.UserId }, cancellationToken: cancellationToken));
            foreach (var code in codes)
            {
                if (recoveryCodeService.VerifyCode(request.RecoveryCode!.Trim(), code.CodeHash))
                {
                    isValid = true;

                    await connection.ExecuteAsync(new CommandDefinition(
                        MfaQueries.MarkRecoveryCodeUsed,
                        new { Id = code.Id, UserId = pending.UserId, UsedAt = now },
                        cancellationToken: cancellationToken));

                    await connection.ExecuteAsync(new CommandDefinition(
                        MfaQueries.InvalidateRecoveryCodes,
                        new { UserId = pending.UserId, InvalidatedAt = now },
                        cancellationToken: cancellationToken));

                    await InsertAuditAsync(connection, new MfaAuditLogEntry
                    {
                        UserId = pending.UserId,
                        ActorUserId = pending.UserId,
                        EventType = "RECOVERY_CODE_USED",
                        EventDescription = "Recovery code consumed",
                        CorrelationId = CorrelationIdMiddleware.GetCorrelationId(httpContext),
                        IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                        CreatedAt = now
                    }, cancellationToken);
                    break;
                }
            }
        }

        if (!isValid)
        {
            var failedAttempts = mfaState.FailedMfaAttempts + 1;
            DateTime? lockoutUntil = null;

            if (failedAttempts >= mfaOptions.Value.MfaMaxAttempts)
            {
                lockoutUntil = now.AddMinutes(mfaOptions.Value.MfaLockoutMinutes);
            }

            await connection.ExecuteAsync(new CommandDefinition(
                MfaQueries.UpdateMfaLockout,
                new { UserId = pending.UserId, FailedMfaAttempts = failedAttempts, MfaLockoutUntil = lockoutUntil, UpdatedAt = now },
                cancellationToken: cancellationToken));

            await InsertAuditAsync(connection, new MfaAuditLogEntry
            {
                UserId = pending.UserId,
                ActorUserId = pending.UserId,
                EventType = lockoutUntil is null ? "MFA_FAILURE" : "MFA_LOCKOUT",
                EventDescription = lockoutUntil is null ? "MFA verification failed" : "MFA lockout applied",
                CorrelationId = CorrelationIdMiddleware.GetCorrelationId(httpContext),
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                CreatedAt = now
            }, cancellationToken);

            if (lockoutUntil is not null)
            {
                return Results.Conflict(new { message = "Conta temporariamente bloqueada. Tente novamente após 15 minutos." });
            }

            return Results.BadRequest(new { message = "Invalid MFA code." });
        }

        await connection.ExecuteAsync(new CommandDefinition(
            MfaQueries.ResetMfaLockout,
            new { UserId = pending.UserId, UpdatedAt = now },
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            MfaQueries.UpdateLastSuccessfulMfa,
            new { UserId = pending.UserId, LastSuccessfulMfaAt = now, UpdatedAt = now },
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            MfaQueries.MarkPendingSessionConsumed,
            new { Id = pendingId, ConsumedAt = now },
            cancellationToken: cancellationToken));

        var user = await connection.QueryFirstOrDefaultAsync<AuthUser>(
            new CommandDefinition(AuthQueries.GetUserById, new { UserId = pending.UserId }, cancellationToken: cancellationToken));

        if (user is null || !user.IsActive)
        {
            return Results.Unauthorized();
        }

        var permissions = (await connection.QueryAsync<string>(
            new CommandDefinition(AuthQueries.GetPermissionsByUserId, new { UserId = user.Id }, cancellationToken: cancellationToken)))
            .ToArray();

        var companies = (await connection.QueryAsync<int>(
            new CommandDefinition(AuthQueries.GetCompanyIdsByUserId, new { UserId = user.Id }, cancellationToken: cancellationToken)))
            .ToArray();

        var token = jwtTokenService.GenerateToken(user, permissions, companies);
        var expiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpirationMinutes);

        await InsertAuditAsync(connection, new MfaAuditLogEntry
        {
            UserId = user.Id,
            ActorUserId = user.Id,
            EventType = "MFA_SUCCESS",
            EventDescription = "MFA verification success",
            CorrelationId = CorrelationIdMiddleware.GetCorrelationId(httpContext),
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            CreatedAt = now
        }, cancellationToken);

        return Results.Ok(new AuthLoginResponse
        {
            Status = "LOGIN_SUCCESS",
            AccessToken = token,
            ExpiresAtUtc = expiresAt,
            Name = user.Name
        });
    }

    private static int ResolveUserId(System.Security.Claims.ClaimsPrincipal user)
    {
        var claim = user.Claims.FirstOrDefault(c => c.Type == "userId" || c.Type == "sub");
        if (claim is null || !int.TryParse(claim.Value, out var id))
        {
            throw new InvalidOperationException("Authenticated user id was not found in token.");
        }

        return id;
    }

    private static string ResolveUserLogin(System.Security.Claims.ClaimsPrincipal user)
    {
        return user.Claims.FirstOrDefault(c => c.Type == PartnerClaimTypes.Login)?.Value ?? "user";
    }

    private static async Task InsertAuditAsync(System.Data.IDbConnection connection, MfaAuditLogEntry entry, CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            MfaQueries.InsertAuditLog,
            new
            {
                entry.UserId,
                entry.ActorUserId,
                entry.EventType,
                entry.EventDescription,
                entry.CorrelationId,
                entry.IpAddress,
                entry.UserAgent,
                entry.MetadataJson,
                entry.CreatedAt
            },
            cancellationToken: cancellationToken));
    }

    private static byte[] GenerateQrCodePng(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(20);
    }

    private static bool TryParsePendingToken(string pendingToken, out Guid id, out string tokenPart)
    {
        id = Guid.Empty;
        tokenPart = string.Empty;
        var parts = pendingToken.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!Guid.TryParse(parts[0], out id))
        {
            return false;
        }

        tokenPart = parts[1];
        return !string.IsNullOrWhiteSpace(tokenPart);
    }
}

public sealed class MfaSetupResponse
{
    public string MaskedSecret { get; init; } = string.Empty;
    public string ManualEntryKey { get; init; } = string.Empty;
    public string QrCodeUrl { get; init; } = string.Empty;
}

public sealed class MfaActivateRequest
{
    public string TotpCode { get; init; } = string.Empty;
}

public sealed class MfaActivateResponse
{
    public IReadOnlyCollection<string> RecoveryCodes { get; init; } = [];
}

public sealed class MfaVerifyRequest
{
    public string PendingToken { get; init; } = string.Empty;
    public string? TotpCode { get; init; }
    public string? RecoveryCode { get; init; }
}