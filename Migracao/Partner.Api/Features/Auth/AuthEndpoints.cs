using Dapper;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Partner.Api.Infrastructure.Database;
using Partner.Api.Infrastructure.Security;

namespace Partner.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
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
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> jwtOptions,
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

        var hasValidPassword = passwordHasher.IsModernHash(user.PasswordHash)
            ? passwordHasher.Verify(request.Password, user.PasswordHash)
            : PasswordVerifier.VerifyLegacyMd5(request.Password, user.PasswordHash);

        if (!hasValidPassword)
        {
            return Results.Unauthorized();
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

        var permissions = (await connection.QueryAsync<string>(
            new CommandDefinition(AuthQueries.GetPermissionsByUserId, new { UserId = user.Id }, cancellationToken: cancellationToken)))
            .ToArray();

        var companies = (await connection.QueryAsync<int>(
            new CommandDefinition(AuthQueries.GetCompanyIdsByUserId, new { UserId = user.Id }, cancellationToken: cancellationToken)))
            .ToArray();

        var token = jwtTokenService.GenerateToken(user, permissions, companies);
        var expiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpirationMinutes);

        return Results.Ok(new AuthLoginResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAt,
            Name = user.Name
        });
    }
}
