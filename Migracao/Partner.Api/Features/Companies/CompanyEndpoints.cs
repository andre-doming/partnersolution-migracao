using Dapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Partner.Api.Infrastructure.Database;
using Partner.Api.Infrastructure.Security;
using Partner.Api.Shared.Security;

namespace Partner.Api.Features.Companies;

public static class CompanyEndpoints
{
    private static readonly HashSet<string> AllowedFilterFields =
    [
        "tradeName",
        "corporateName",
        "cnpj",
        "managerName"
    ];

    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/companies")
            .WithTags("Companies")
            .RequireAuthorization(AuthPolicies.Companies);

        group.MapGet("/", ListAsync)
            .Produces<CompanyListResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:int}", GetByIdAsync)
            .Produces<CompanyDetailResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(AuthPolicies.CompaniesInsert)
            .Produces<CompanyDetailResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:int}", UpdateAsync)
            .RequireAuthorization(AuthPolicies.CompaniesUpdate)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", DeleteAsync)
            .RequireAuthorization(AuthPolicies.CompaniesDelete)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] CompanyListRequest request,
        IValidator<CompanyListRequest> validator,
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
            {CompanyQueries.ListPagedBase}
            {whereClause}
            ORDER BY e.nome_fantasia
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;
            """;

        var countSql = $"""
            {CompanyQueries.CountBase}
            {whereClause};
            """;

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", request.PageSize);

        using var connection = connectionFactory.CreateConnection();

        var items = (await connection.QueryAsync<CompanyRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))).ToList();

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        return Results.Ok(new CompanyListResponse
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total,
            Items = items.Select(x => new CompanyListItemResponse
            {
                Id = x.Id,
                Cnpj = x.Cnpj,
                TradeName = x.TradeName,
                CorporateName = x.CorporateName,
                ManagerName = x.ManagerName,
                IsActive = x.IsActive
            }).ToArray()
        });
    }

    private static async Task<IResult> GetByIdAsync(
        [FromRoute] int id,
        ISqlConnectionFactory connectionFactory,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        var company = await connection.QueryFirstOrDefaultAsync<CompanyRow>(
            new CommandDefinition(CompanyQueries.GetById, new { Id = id }, cancellationToken: cancellationToken));

        if (company is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new CompanyDetailResponse
        {
            Id = company.Id,
            Cnpj = company.Cnpj,
            TradeName = company.TradeName,
            CorporateName = company.CorporateName,
            ManagerName = company.ManagerName,
            IsActive = company.IsActive
        });
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CompanyUpsertRequest request,
        IValidator<CompanyUpsertRequest> validator,
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

        var normalizedCnpj = CompanyCnpjValidator.Normalize(request.Cnpj);
        await EnsureBusinessRulesAsync(connection, normalizedCnpj, null, cancellationToken);

        var actorUserId = ResolveActorUserId(httpContext.User);
        var companyId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            CompanyQueries.Insert,
            new
            {
                Cnpj = normalizedCnpj,
                request.TradeName,
                request.CorporateName,
                request.ManagerName,
                Active = request.IsActive ? "S" : "N",
                ActorUserId = actorUserId
            },
            cancellationToken: cancellationToken));

        return Results.Created($"/api/companies/{companyId}", new CompanyDetailResponse
        {
            Id = companyId,
            Cnpj = normalizedCnpj,
            TradeName = request.TradeName,
            CorporateName = request.CorporateName,
            ManagerName = request.ManagerName,
            IsActive = request.IsActive
        });
    }

    private static async Task<IResult> UpdateAsync(
        [FromRoute] int id,
        [FromBody] CompanyUpsertRequest request,
        IValidator<CompanyUpsertRequest> validator,
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
            new CommandDefinition(CompanyQueries.CountBase + " AND e.id = @Id;", new { Id = id }, cancellationToken: cancellationToken));

        if (exists == 0)
        {
            return Results.NotFound();
        }

        var normalizedCnpj = CompanyCnpjValidator.Normalize(request.Cnpj);
        await EnsureBusinessRulesAsync(connection, normalizedCnpj, id, cancellationToken);

        var actorUserId = ResolveActorUserId(httpContext.User);
        await connection.ExecuteAsync(new CommandDefinition(
            CompanyQueries.Update,
            new
            {
                Id = id,
                Cnpj = normalizedCnpj,
                request.TradeName,
                request.CorporateName,
                request.ManagerName,
                Active = request.IsActive ? "S" : "N",
                ActorUserId = actorUserId
            },
            cancellationToken: cancellationToken));

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
            new CommandDefinition(CompanyQueries.CountBase + " AND e.id = @Id;", new { Id = id }, cancellationToken: cancellationToken));

        if (exists == 0)
        {
            return Results.NotFound();
        }

        var actorUserId = ResolveActorUserId(httpContext.User);

        await connection.ExecuteAsync(new CommandDefinition(
            CompanyQueries.Inactivate,
            new { Id = id, ActorUserId = actorUserId },
            cancellationToken: cancellationToken));

        return Results.NoContent();
    }

    private static string BuildWhereClause(CompanyListRequest request, out DynamicParameters parameters)
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
                    "tradeName" => "e.nome_fantasia LIKE @Term",
                    "corporateName" => "e.razao_social LIKE @Term",
                    "cnpj" => "e.cnpj LIKE @Term",
                    "managerName" => "e.gerente_responsavel LIKE @Term",
                    _ => throw new InvalidOperationException("Unsupported field.")
                });

                parameters.Add("Term", $"%{normalized}%");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Active))
        {
            where.Add("e.ativo = @Active");
            parameters.Add("Active", request.Active);
        }

        if (where.Count == 0)
        {
            return string.Empty;
        }

        return " AND " + string.Join(" AND ", where);
    }

    private static async Task EnsureBusinessRulesAsync(
        System.Data.IDbConnection connection,
        string normalizedCnpj,
        int? companyId,
        CancellationToken cancellationToken)
    {
        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(CompanyQueries.ExistsCnpj, new { Cnpj = normalizedCnpj, Id = companyId }, cancellationToken: cancellationToken));

        if (count > 0)
        {
            throw new ValidationException("CNPJ already exists for an active company.");
        }
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
