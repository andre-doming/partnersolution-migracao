using Dapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Partner.Api.Infrastructure.Database;
using Partner.Api.Infrastructure.Security;
using Partner.Api.Shared.Security;

namespace Partner.Api.Features.Clients;

public static class ClientEndpoints
{
    private static readonly HashSet<string> AllowedFilterFields =
    [
        "firstName",
        "lastName",
        "document",
        "email",
        "companyName",
        "role"
    ];

    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clients")
            .WithTags("Clients")
            .RequireAuthorization(AuthPolicies.Clients);

        group.MapGet("/", ListAsync)
            .Produces<ClientListResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/lookups", GetLookupsAsync)
            .Produces<ClientLookupResponse>(StatusCodes.Status200OK);

        group.MapGet("/{id:int}", GetByIdAsync)
            .Produces<ClientDetailResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(AuthPolicies.ClientsInsert)
            .Produces<ClientDetailResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:int}", UpdateAsync)
            .RequireAuthorization(AuthPolicies.ClientsUpdate)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", DeleteAsync)
            .RequireAuthorization(AuthPolicies.ClientsDelete)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] ClientListRequest request,
        IValidator<ClientListRequest> validator,
        ISqlConnectionFactory connectionFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var isAdmin = IsAdmin(httpContext.User);
        var allowedCompanies = GetAllowedCompanyIds(httpContext.User);

        var whereClause = BuildWhereClause(request, isAdmin, allowedCompanies, out var parameters);
        var offset = (request.Page - 1) * request.PageSize;

        var sql = $"""
            {ClientQueries.ListPagedBase}
            {whereClause}
            ORDER BY c.id DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;
            """;

        var countSql = $"""
            {ClientQueries.CountBase}
            {whereClause};
            """;

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", request.PageSize);

        using var connection = connectionFactory.CreateConnection();

        var items = (await connection.QueryAsync<ClientRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))).ToArray();

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        return Results.Ok(new ClientListResponse
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total,
            Items = items.Select(x => new ClientListItemResponse
            {
                Id = x.Id,
                ClientGuid = x.ClientGuid,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Document = x.Document,
                Email = x.Email,
                Gender = x.Gender,
                BirthDate = x.BirthDate,
                CompanyId = x.CompanyId,
                CompanyName = x.CompanyName,
                Department = x.Department,
                Role = x.Role,
                Approved = x.Approved,
                IsActive = x.IsActive
            }).ToArray()
        });
    }

    private static async Task<IResult> GetByIdAsync(
        [FromRoute] int id,
        ISqlConnectionFactory connectionFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        var row = await connection.QueryFirstOrDefaultAsync<ClientRow>(
            new CommandDefinition(ClientQueries.GetById, new { Id = id }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return Results.NotFound();
        }

        if (!CanAccessCompany(httpContext.User, row.CompanyId))
        {
            return Results.NotFound();
        }

        return Results.Ok(ToDetail(row));
    }

    private static async Task<IResult> GetLookupsAsync(
        ISqlConnectionFactory connectionFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var isAdmin = IsAdmin(httpContext.User);
        var allowedCompanies = GetAllowedCompanyIds(httpContext.User);

        using var connection = connectionFactory.CreateConnection();

        IReadOnlyCollection<ClientCompanyLookupItem> companies;

        if (isAdmin)
        {
            companies = (await connection.QueryAsync<ClientCompanyLookupItem>(
                new CommandDefinition(ClientQueries.LookupCompanies, cancellationToken: cancellationToken))).ToArray();
        }
        else
        {
            if (allowedCompanies.Count == 0)
            {
                companies = [];
            }
            else
            {
                var sql = """
                    SELECT
                        e.id AS Id,
                        e.nome_fantasia AS Name
                    FROM tb_empresa e
                    WHERE e.ativo = 'S'
                      AND e.id IN @CompanyIds
                    ORDER BY e.nome_fantasia;
                    """;
                companies = (await connection.QueryAsync<ClientCompanyLookupItem>(
                    new CommandDefinition(sql, new { CompanyIds = allowedCompanies }, cancellationToken: cancellationToken))).ToArray();
            }
        }

        return Results.Ok(new ClientLookupResponse { Companies = companies });
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] ClientUpsertRequest request,
        IValidator<ClientUpsertRequest> validator,
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

        var companyMap = await EnsureBusinessRulesAsync(connection, httpContext.User, request, null, cancellationToken);
        var normalizedDocument = ClientDocumentNormalizer.Normalize(request.Document);
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var clientGuid = Guid.NewGuid();
        var clientId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            ClientQueries.Insert,
            new
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Document = normalizedDocument,
                Email = normalizedEmail,
                CompanyName = companyMap.CompanyName,
                PartnerId = companyMap.PartnerId,
                Gender = request.Gender?.Trim(),
                BirthDate = request.BirthDate?.Trim(),
                Department = request.Department?.Trim(),
                Role = request.Role?.Trim(),
                request.Approved,
                Active = request.IsActive ? "S" : "N",
                ClientGuid = clientGuid
            },
            cancellationToken: cancellationToken));

        return Results.Created($"/api/clients/{clientId}", new ClientDetailResponse
        {
            Id = clientId,
            ClientGuid = clientGuid,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Document = normalizedDocument,
            Email = normalizedEmail,
            Gender = request.Gender?.Trim(),
            BirthDate = request.BirthDate?.Trim(),
            CompanyId = companyMap.CompanyId,
            CompanyName = companyMap.CompanyName,
            Department = request.Department?.Trim(),
            Role = request.Role?.Trim(),
            Approved = request.Approved,
            IsActive = request.IsActive
        });
    }

    private static async Task<IResult> UpdateAsync(
        [FromRoute] int id,
        [FromBody] ClientUpsertRequest request,
        IValidator<ClientUpsertRequest> validator,
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

        var current = await connection.QueryFirstOrDefaultAsync<ClientRow>(
            new CommandDefinition(ClientQueries.GetById, new { Id = id }, cancellationToken: cancellationToken));

        if (current is null)
        {
            return Results.NotFound();
        }

        if (!CanAccessCompany(httpContext.User, current.CompanyId))
        {
            return Results.NotFound();
        }

        var companyMap = await EnsureBusinessRulesAsync(connection, httpContext.User, request, id, cancellationToken);
        var normalizedDocument = ClientDocumentNormalizer.Normalize(request.Document);
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        await connection.ExecuteAsync(new CommandDefinition(
            ClientQueries.Update,
            new
            {
                Id = id,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Document = normalizedDocument,
                Email = normalizedEmail,
                CompanyName = companyMap.CompanyName,
                PartnerId = companyMap.PartnerId,
                Gender = request.Gender?.Trim(),
                BirthDate = request.BirthDate?.Trim(),
                Department = request.Department?.Trim(),
                Role = request.Role?.Trim(),
                request.Approved,
                Active = request.IsActive ? "S" : "N"
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

        var current = await connection.QueryFirstOrDefaultAsync<ClientRow>(
            new CommandDefinition(ClientQueries.GetById, new { Id = id }, cancellationToken: cancellationToken));

        if (current is null)
        {
            return Results.NotFound();
        }

        if (!CanAccessCompany(httpContext.User, current.CompanyId))
        {
            return Results.NotFound();
        }

        await connection.ExecuteAsync(new CommandDefinition(
            ClientQueries.Inactivate,
            new { Id = id },
            cancellationToken: cancellationToken));

        return Results.NoContent();
    }

    private static string BuildWhereClause(
        ClientListRequest request,
        bool isAdmin,
        IReadOnlyCollection<int> allowedCompanyIds,
        out DynamicParameters parameters)
    {
        parameters = new DynamicParameters();
        var where = new List<string>();

        if (!isAdmin)
        {
            if (allowedCompanyIds.Count == 0)
            {
                where.Add("1 = 0");
            }
            else
            {
                where.Add("e.id IN @AllowedCompanyIds");
                parameters.Add("AllowedCompanyIds", allowedCompanyIds.ToArray());
            }
        }

        if (request.CompanyId is > 0)
        {
            if (!isAdmin && !allowedCompanyIds.Contains(request.CompanyId.Value))
            {
                throw new ValidationException("Selected company is not available for the authenticated user.");
            }

            where.Add("e.id = @CompanyId");
            parameters.Add("CompanyId", request.CompanyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Field) && !string.IsNullOrWhiteSpace(request.Value))
        {
            var selectedField = FilterSanitizer.EnsureAllowedField(request.Field, AllowedFilterFields.ToArray());
            var normalized = FilterSanitizer.NormalizeTerm(request.Value, 80);

            if (!string.IsNullOrWhiteSpace(normalized))
            {
                if (selectedField == "document")
                {
                    var digits = new string(normalized.Where(char.IsDigit).ToArray());
                    if (!string.IsNullOrWhiteSpace(digits))
                    {
                        where.Add("(REPLACE(REPLACE(REPLACE(c.cpf, '.', ''), '-', ''), '/', '') LIKE @DocumentDigitsLike OR LOWER(c.cpf) = LOWER(CONVERT(varchar(32), HASHBYTES('MD5', @DocumentDigits), 2)))");
                        parameters.Add("Term", $"%{normalized}%");
                        parameters.Add("DocumentDigitsLike", $"%{digits}%");
                        parameters.Add("DocumentDigits", digits);
                    }
                    else
                    {
                        where.Add("c.cpf LIKE @Term");
                        parameters.Add("Term", $"%{normalized}%");
                    }
                }
                else
                {
                    where.Add(selectedField switch
                    {
                        "firstName" => "c.nome LIKE @Term",
                        "lastName" => "c.sobrenome LIKE @Term",
                        "email" => "c.email LIKE @Term",
                        "companyName" => "e.nome_fantasia LIKE @Term",
                        "role" => "c.cargo LIKE @Term",
                        _ => throw new InvalidOperationException("Unsupported field.")
                    });

                    parameters.Add("Term", $"%{normalized}%");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Active))
        {
            where.Add("c.ativo = @Active");
            parameters.Add("Active", request.Active);
        }

        if (where.Count == 0)
        {
            return string.Empty;
        }

        return " AND " + string.Join(" AND ", where);
    }

    private static async Task<ClientCompanyMapRow> EnsureBusinessRulesAsync(
        System.Data.IDbConnection connection,
        System.Security.Claims.ClaimsPrincipal user,
        ClientUpsertRequest request,
        int? clientId,
        CancellationToken cancellationToken)
    {
        var companyExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            ClientQueries.CountCompanyById,
            new { request.CompanyId },
            cancellationToken: cancellationToken));

        if (companyExists == 0)
        {
            throw new ValidationException("Company is invalid or inactive.");
        }

        if (!IsAdmin(user))
        {
            var userId = ResolveActorUserId(user);
            var hasAccess = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                ClientQueries.CountUserAccessToCompany,
                new { UserId = userId, request.CompanyId },
                cancellationToken: cancellationToken));

            if (hasAccess == 0)
            {
                throw new ValidationException("Selected company is not available for the authenticated user.");
            }
        }

        var companyMap = await connection.QueryFirstOrDefaultAsync<ClientCompanyMapRow>(new CommandDefinition(
            ClientQueries.GetCompanyMapById,
            new { request.CompanyId },
            cancellationToken: cancellationToken));

        if (companyMap is null)
        {
            throw new ValidationException("Company data could not be resolved.");
        }

        var normalizedDocument = ClientDocumentNormalizer.Normalize(request.Document);
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var documentCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            ClientQueries.ExistsDocument,
            new { Document = normalizedDocument, companyMap.PartnerId, Id = clientId },
            cancellationToken: cancellationToken));

        if (documentCount > 0)
        {
            throw new ValidationException("Document already exists for an active client in the selected company.");
        }

        var emailCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            ClientQueries.ExistsEmail,
            new { Email = normalizedEmail, companyMap.PartnerId, Id = clientId },
            cancellationToken: cancellationToken));

        if (emailCount > 0)
        {
            throw new ValidationException("E-mail already exists for an active client in the selected company.");
        }

        return companyMap;
    }

    private static ClientDetailResponse ToDetail(ClientRow row) => new()
    {
        Id = row.Id,
        ClientGuid = row.ClientGuid,
        FirstName = row.FirstName,
        LastName = row.LastName,
        Document = row.Document,
        Email = row.Email,
        Gender = row.Gender,
        BirthDate = row.BirthDate,
        CompanyId = row.CompanyId,
        CompanyName = row.CompanyName,
        Department = row.Department,
        Role = row.Role,
        Approved = row.Approved,
        IsActive = row.IsActive
    };

    private static bool IsAdmin(System.Security.Claims.ClaimsPrincipal user)
    {
        return user.Claims.Any(c =>
            c.Type == PartnerClaimTypes.Admin &&
            string.Equals(c.Value, "true", StringComparison.OrdinalIgnoreCase));
    }

    private static HashSet<int> GetAllowedCompanyIds(System.Security.Claims.ClaimsPrincipal user)
    {
        var ids = new HashSet<int>();

        foreach (var claim in user.Claims.Where(c => c.Type == PartnerClaimTypes.Companies))
        {
            if (int.TryParse(claim.Value, out var id) && id > 0)
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private static bool CanAccessCompany(System.Security.Claims.ClaimsPrincipal user, int companyId)
    {
        if (IsAdmin(user))
        {
            return true;
        }

        return GetAllowedCompanyIds(user).Contains(companyId);
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
