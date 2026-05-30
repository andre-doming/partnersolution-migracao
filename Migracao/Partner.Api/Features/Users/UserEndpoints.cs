using Dapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Partner.Api.Features.Auth;
using Partner.Api.Infrastructure.Database;
using Partner.Api.Infrastructure.Security;
using Partner.Api.Shared.Security;
using System.Data;

namespace Partner.Api.Features.Users;

public static class UserEndpoints
{
    private static readonly HashSet<string> AllowedFilterFields =
    [
        "name",
        "login",
        "email"
    ];

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization(AuthPolicies.Users);

        group.MapGet("/", ListAsync)
            .Produces<UserListResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/lookups", GetLookupsAsync)
            .Produces<UserLookupResponse>(StatusCodes.Status200OK);

        group.MapGet("/{id:int}", GetByIdAsync)
            .Produces<UserDetailResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(AuthPolicies.UsersInsert)
            .Produces<UserDetailResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:int}", UpdateAsync)
            .RequireAuthorization(AuthPolicies.UsersUpdate)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", DeleteAsync)
            .RequireAuthorization(AuthPolicies.UsersDelete)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] UserListRequest request,
        IValidator<UserListRequest> validator,
        ISqlConnectionFactory connectionFactory,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var whereClause = BuildWhereClause(request, out var parameters);
        var offset = (request.Page - 1) * request.PageSize;

        var sql = $"""
            {UserQueries.ListPagedBase}
            {whereClause}
            ORDER BY u.nome
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;
            """;

        var countSql = $"""
            {UserQueries.CountBase}
            {whereClause};
            """;

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", request.PageSize);

        using var connection = connectionFactory.CreateConnection();

        var items = (await connection.QueryAsync<UserListRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))).ToList();

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        var resultItems = items.Select(x => new UserListItemResponse
        {
            Id = x.Id,
            Name = x.Name,
            Login = x.Login,
            Email = x.Email,
            IsAdmin = x.IsAdmin,
            IsActive = x.IsActive,
            AccessToken = x.AccessToken
        }).ToList();

        if (resultItems.Count > 0)
        {
            var ids = resultItems.Select(x => x.Id).ToArray();

            var companyLinks = (await connection.QueryAsync<UserIdLinkRow>(
                new CommandDefinition(UserQueries.GetCompanyIdsByUserIds, new { UserIds = ids }, cancellationToken: cancellationToken)))
                .GroupBy(x => x.UserId)
                .ToDictionary(x => x.Key, x => (IReadOnlyCollection<int>)x.Select(v => v.Id).Distinct().ToArray());

            var permissionLinks = (await connection.QueryAsync<UserPermissionLinkRow>(
                new CommandDefinition(UserQueries.GetPermissionCodesByUserIds, new { UserIds = ids }, cancellationToken: cancellationToken)))
                .GroupBy(x => x.UserId)
                .ToDictionary(x => x.Key, x => (IReadOnlyCollection<string>)x.Select(v => v.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());

            foreach (var item in resultItems)
            {
                item.CompanyIds = companyLinks.TryGetValue(item.Id, out var companyIds) ? companyIds : [];
                item.Permissions = permissionLinks.TryGetValue(item.Id, out var permissions) ? permissions : [];
            }
        }

        return Results.Ok(new UserListResponse
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total,
            Items = resultItems
        });
    }

    private static async Task<IResult> GetByIdAsync(
        [FromRoute] int id,
        ISqlConnectionFactory connectionFactory,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        var user = await connection.QueryFirstOrDefaultAsync<UserDetailRow>(
            new CommandDefinition(UserQueries.GetById, new { Id = id }, cancellationToken: cancellationToken));

        if (user is null)
        {
            return Results.NotFound();
        }

        var companyIds = (await connection.QueryAsync<int>(
            new CommandDefinition(UserQueries.GetCompanyIdsByUserId, new { UserId = id }, cancellationToken: cancellationToken))).ToArray();

        var functionIds = (await connection.QueryAsync<int>(
            new CommandDefinition(UserQueries.GetFunctionIdsByUserId, new { UserId = id }, cancellationToken: cancellationToken))).ToArray();

        return Results.Ok(new UserDetailResponse
        {
            Id = user.Id,
            Name = user.Name,
            Login = user.Login,
            Email = user.Email,
            IsAdmin = user.IsAdmin,
            IsActive = user.IsActive,
            AccessToken = user.AccessToken,
            FirstAccess = user.FirstAccess,
            CompanyIds = companyIds,
            FunctionIds = functionIds
        });
    }

    private static async Task<IResult> GetLookupsAsync(
        ISqlConnectionFactory connectionFactory,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        var companies = (await connection.QueryAsync<UserCompanyLookupItem>(
            new CommandDefinition(UserQueries.LookupCompanies, cancellationToken: cancellationToken))).ToArray();

        var functions = (await connection.QueryAsync<UserFunctionLookupItem>(
            new CommandDefinition(UserQueries.LookupFunctions, cancellationToken: cancellationToken))).ToArray();

        return Results.Ok(new UserLookupResponse
        {
            Companies = companies,
            Functions = functions
        });
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] UserUpsertRequest request,
        IValidator<UserUpsertRequest> validator,
        ISqlConnectionFactory connectionFactory,
        IPasswordHasher passwordHasher,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        using var connection = connectionFactory.CreateConnection();
        await EnsureBusinessRulesAsync(connection, request, null, cancellationToken);

        var createdBy = ResolveActorUserId(httpContext.User);
        var password = string.IsNullOrWhiteSpace(request.Password) ? "Temp#123456" : request.Password.Trim();
        var passwordHash = passwordHasher.Hash(password);

        var userId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            UserQueries.InsertUser,
            new
            {
                request.Name,
                request.Login,
                request.Email,
                PasswordHash = passwordHash,
                FirstAccess = "S",
                AccessToken = request.AccessToken ? "S" : "N",
                Admin = request.IsAdmin ? "S" : "N",
                Active = request.IsActive ? "S" : "N",
                CreatedByUserId = createdBy
            },
            cancellationToken: cancellationToken));

        await ReplaceLinksAsync(connection, userId, request.CompanyIds, request.FunctionIds, cancellationToken);

        return Results.Created($"/api/users/{userId}", new UserDetailResponse
        {
            Id = userId,
            Name = request.Name,
            Login = request.Login,
            Email = request.Email,
            IsAdmin = request.IsAdmin,
            IsActive = request.IsActive,
            AccessToken = request.AccessToken,
            FirstAccess = true,
            CompanyIds = request.CompanyIds.Distinct().ToArray(),
            FunctionIds = request.FunctionIds.Distinct().ToArray()
        });
    }

    private static async Task<IResult> UpdateAsync(
        [FromRoute] int id,
        [FromBody] UserUpsertRequest request,
        IValidator<UserUpsertRequest> validator,
        ISqlConnectionFactory connectionFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        using var connection = connectionFactory.CreateConnection();

        var exists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(UserQueries.CountBase + " AND u.id = @Id;", new { Id = id }, cancellationToken: cancellationToken));

        if (exists == 0)
        {
            return Results.NotFound();
        }

        await EnsureBusinessRulesAsync(connection, request, id, cancellationToken);

        var updatedBy = ResolveActorUserId(httpContext.User);

        await connection.ExecuteAsync(new CommandDefinition(
            UserQueries.UpdateUser,
            new
            {
                Id = id,
                request.Name,
                request.Login,
                request.Email,
                AccessToken = request.AccessToken ? "S" : "N",
                Admin = request.IsAdmin ? "S" : "N",
                Active = request.IsActive ? "S" : "N",
                UpdatedByUserId = updatedBy
            },
            cancellationToken: cancellationToken));

        await ReplaceLinksAsync(connection, id, request.CompanyIds, request.FunctionIds, cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAsync(
        [FromRoute] int id,
        ISqlConnectionFactory connectionFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        var exists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(UserQueries.CountBase + " AND u.id = @Id;", new { Id = id }, cancellationToken: cancellationToken));

        if (exists == 0)
        {
            return Results.NotFound();
        }

        var updatedBy = ResolveActorUserId(httpContext.User);
        await connection.ExecuteAsync(new CommandDefinition(
            UserQueries.InactivateUser,
            new { Id = id, UpdatedByUserId = updatedBy },
            cancellationToken: cancellationToken));

        return Results.NoContent();
    }

    private static string BuildWhereClause(UserListRequest request, out DynamicParameters parameters)
    {
        parameters = new DynamicParameters();
        var where = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.Field) && !string.IsNullOrWhiteSpace(request.Value))
        {
            var selectedField = FilterSanitizer.EnsureAllowedField(request.Field, AllowedFilterFields.ToArray());
            var normalized = FilterSanitizer.NormalizeTerm(request.Value, 80);

            if (!string.IsNullOrWhiteSpace(normalized))
            {
                where.Add(selectedField switch
                {
                    "name" => "u.nome LIKE @Term",
                    "login" => "u.login LIKE @Term",
                    "email" => "u.email LIKE @Term",
                    _ => throw new InvalidOperationException("Unsupported field.")
                });

                parameters.Add("Term", $"%{normalized}%");
            }
        }

        if (request.CompanyId is > 0)
        {
            where.Add("EXISTS (SELECT 1 FROM tb_empresa_usuario eu WHERE eu.id_usuario = u.id AND eu.id_empresa = @CompanyId)");
            parameters.Add("CompanyId", request.CompanyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Active))
        {
            where.Add("u.ativo = @Active");
            parameters.Add("Active", request.Active);
        }

        if (where.Count == 0)
        {
            return string.Empty;
        }

        return " AND " + string.Join(" AND ", where);
    }

    private static async Task EnsureBusinessRulesAsync(IDbConnection connection, UserUpsertRequest request, int? userId, CancellationToken cancellationToken)
    {
        var companyIds = request.CompanyIds.Distinct().ToArray();
        var functionIds = request.FunctionIds.Distinct().ToArray();

        if (companyIds.Length == 0)
        {
            throw new ValidationException("At least one company must be selected.");
        }

        if (functionIds.Length == 0)
        {
            throw new ValidationException("At least one permission must be selected.");
        }

        var loginCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(UserQueries.ExistsLogin, new { request.Login, UserId = userId }, cancellationToken: cancellationToken));

        if (loginCount > 0)
        {
            throw new ValidationException("Login already exists for an active user.");
        }

        var emailCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(UserQueries.ExistsEmail, new { request.Email, UserId = userId }, cancellationToken: cancellationToken));

        if (emailCount > 0)
        {
            throw new ValidationException("E-mail already exists for an active user.");
        }

        var companyCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(UserQueries.CountActiveCompanies, new { CompanyIds = companyIds }, cancellationToken: cancellationToken));

        if (companyCount != companyIds.Length)
        {
            throw new ValidationException("One or more companies are invalid or inactive.");
        }

        var functionCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(UserQueries.CountActiveFunctions, new { FunctionIds = functionIds }, cancellationToken: cancellationToken));

        if (functionCount != functionIds.Length)
        {
            throw new ValidationException("One or more permissions are invalid or inactive.");
        }
    }

    private static async Task ReplaceLinksAsync(
        IDbConnection connection,
        int userId,
        IReadOnlyCollection<int> companyIds,
        IReadOnlyCollection<int> functionIds,
        CancellationToken cancellationToken)
    {
        using var tx = connection.BeginTransaction();

        await connection.ExecuteAsync(new CommandDefinition(
            UserQueries.DeleteUserCompanies,
            new { UserId = userId },
            tx,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            UserQueries.DeleteUserFunctions,
            new { UserId = userId },
            tx,
            cancellationToken: cancellationToken));

        foreach (var companyId in companyIds.Distinct())
        {
            await connection.ExecuteAsync(new CommandDefinition(
                UserQueries.InsertUserCompany,
                new { UserId = userId, CompanyId = companyId },
                tx,
                cancellationToken: cancellationToken));
        }

        foreach (var functionId in functionIds.Distinct())
        {
            await connection.ExecuteAsync(new CommandDefinition(
                UserQueries.InsertUserFunction,
                new { UserId = userId, FunctionId = functionId },
                tx,
                cancellationToken: cancellationToken));
        }

        tx.Commit();
    }

    private static int ResolveActorUserId(System.Security.Claims.ClaimsPrincipal user)
    {
        var claim = user.Claims.FirstOrDefault(c => c.Type == PartnerClaimTypes.UserId || c.Type == "sub");
        if (claim is null || !int.TryParse(claim.Value, out var id))
        {
            throw new ValidationException("Authenticated user id was not found in token.");
        }

        return id;
    }
}
