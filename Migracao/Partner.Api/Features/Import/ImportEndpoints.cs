using Dapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Partner.Api.Features.Clients;
using Partner.Api.Infrastructure.Database;
using Partner.Api.Infrastructure.Observability;
using Partner.Api.Infrastructure.Security;
using Partner.Api.Middleware;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Partner.Api.Features.Import;

public static class ImportEndpoints
{
    private static readonly string[] ExpectedColumns =
    [
        "nome",
        "sobrenome",
        "cpf",
        "email",
        "sexo",
        "dt_nascimento",
        "departamento",
        "cargo",
        "acao"
    ];

    public static IEndpointRouteBuilder MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/import")
            .WithTags("Import")
            .RequireAuthorization(AuthPolicies.Import);

        group.MapPost("/clients-csv", ImportClientsCsvAsync)
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ImportClientsCsvResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/clients-csv/preview", PreviewClientsCsvAsync)
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ImportPreviewCsvResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/clients-csv/process-selected", ProcessSelectedClientsCsvAsync)
            .Accepts<ImportProcessSelectedRequest>("application/json")
            .Produces<ImportClientsCsvResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/lookups", GetLookupsAsync)
            .Produces<ClientLookupResponse>(StatusCodes.Status200OK);

        group.MapGet("/jobs", ListJobsAsync)
            .Produces<ImportJobListResponse>(StatusCodes.Status200OK);

        group.MapGet("/jobs/{id:int}", GetJobByIdAsync)
            .Produces<ImportJobDetailResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ImportClientsCsvAsync(
        HttpContext httpContext,
        ISqlConnectionFactory connectionFactory,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("ImportCsvClients");
        var startedAtUtc = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationIdMiddleware.GetCorrelationId(httpContext);

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(ImportQueries.EnsureImportTables, cancellationToken: cancellationToken));

        if (!httpContext.Request.HasFormContentType)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["Request must be multipart/form-data."]
            });
        }

        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");

        if (file is null || file.Length == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["CSV file is required."]
            });
        }

        if (!int.TryParse(form["companyId"], out var companyId) || companyId <= 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["companyId"] = ["companyId is required and must be greater than zero."]
            });
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["CSV file must be up to 5MB."]
            });
        }

        var actorUserId = ResolveActorUserId(httpContext.User);
        var isAdmin = IsAdmin(httpContext.User);

        var companyMap = await connection.QueryFirstOrDefaultAsync<ImportCompanyMapRow>(
            new CommandDefinition(ImportQueries.GetCompanyMapById, new { CompanyId = companyId }, cancellationToken: cancellationToken));

        if (companyMap is null)
        {
            throw new ValidationException("Company is invalid or inactive.");
        }

        if (!isAdmin)
        {
            var hasAccess = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(ImportQueries.CountUserAccessToCompany, new { UserId = actorUserId, CompanyId = companyId }, cancellationToken: cancellationToken));

            if (hasAccess == 0)
            {
                throw new ValidationException("Selected company is not available for the authenticated user.");
            }
        }

        var jobId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            ImportQueries.InsertImportJob,
            new
            {
                Feature = "clients-csv",
                FileName = file.FileName,
                CompanyId = companyId,
                Status = "processing",
                TotalRows = 0,
                SuccessRows = 0,
                ErrorRows = 0,
                DurationMs = 0,
                StartedAtUtc = startedAtUtc,
                FinishedAtUtc = (DateTime?)null,
                CreatedByUserId = actorUserId
            },
            cancellationToken: cancellationToken));

        ImportJobMetrics.MarkStarted();
        logger.LogInformation(
            "Import job started. JobId={JobId} CompanyId={CompanyId} ActorUserId={ActorUserId} FileName={FileName} CorrelationId={CorrelationId}",
            jobId,
            companyId,
            actorUserId,
            file.FileName,
            correlationId);

        var rowErrors = new List<ImportRowErrorResponse>();
        var successRows = 0;
        var totalRows = 0;

        try
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            var headerLine = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                throw new ValidationException("CSV header is empty.");
            }

            ValidateHeader(headerLine);

            var lineNumber = 1;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                totalRows++;

                try
                {
                    var data = ParseLine(lineNumber, line);
                    await ProcessLineAsync(connection, companyMap, data, cancellationToken);
                    successRows++;
                }
                catch (Exception exception)
                {
                    var parsed = TryParseLoosely(lineNumber, line);
                    var error = new ImportRowErrorResponse
                    {
                        LineNumber = lineNumber,
                        Action = parsed?.Action,
                        Document = parsed?.Document,
                        Email = parsed?.Email,
                        Message = exception.Message
                    };

                    rowErrors.Add(error);

                    logger.LogWarning(exception,
                        "Import line failed. JobId={JobId} CompanyId={CompanyId} ActorUserId={ActorUserId} Line={Line} Action={Action} Document={Document} Email={Email} CorrelationId={CorrelationId}",
                        jobId,
                        companyId,
                        actorUserId,
                        lineNumber,
                        error.Action,
                        error.Document,
                        error.Email,
                        correlationId);
                }
            }

            stopwatch.Stop();
            var status = rowErrors.Count == 0 ? "completed" : successRows == 0 ? "failed" : "completed_with_errors";

            await connection.ExecuteAsync(new CommandDefinition(
                ImportQueries.UpdateImportJob,
                new
                {
                    Id = jobId,
                    Status = status,
                    TotalRows = totalRows,
                    SuccessRows = successRows,
                    ErrorRows = rowErrors.Count,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    FinishedAtUtc = DateTime.UtcNow
                },
                cancellationToken: cancellationToken));

            foreach (var error in rowErrors)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    ImportQueries.InsertImportJobError,
                    new
                    {
                        ImportJobId = jobId,
                        error.LineNumber,
                        error.Action,
                        error.Document,
                        error.Email,
                        error.Message
                    },
                    cancellationToken: cancellationToken));
            }

            logger.LogInformation(
                "Import job finished. JobId={JobId} CompanyId={CompanyId} ActorUserId={ActorUserId} FileName={FileName} Status={Status} TotalRows={TotalRows} SuccessRows={SuccessRows} ErrorRows={ErrorRows} DurationMs={DurationMs} CorrelationId={CorrelationId} Metrics={Metrics}",
                jobId,
                companyId,
                actorUserId,
                file.FileName,
                status,
                totalRows,
                successRows,
                rowErrors.Count,
                (int)stopwatch.ElapsedMilliseconds,
                correlationId,
                ImportJobMetrics.Snapshot());

            ImportJobMetrics.MarkCompleted(failed: string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase));

            return Results.Ok(new ImportClientsCsvResponse
            {
                JobId = jobId,
                Status = status,
                FileName = file.FileName,
                CompanyId = companyId,
                TotalRows = totalRows,
                SuccessRows = successRows,
                ErrorRows = rowErrors.Count,
                DurationMs = (int)stopwatch.ElapsedMilliseconds,
                Errors = rowErrors
            });
        }
        catch
        {
            stopwatch.Stop();

            ImportJobMetrics.MarkCompleted(failed: true);

            await connection.ExecuteAsync(new CommandDefinition(
                ImportQueries.UpdateImportJob,
                new
                {
                    Id = jobId,
                    Status = "failed",
                    TotalRows = totalRows,
                    SuccessRows = successRows,
                    ErrorRows = rowErrors.Count,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    FinishedAtUtc = DateTime.UtcNow
                },
                cancellationToken: cancellationToken));

            throw;
        }
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
        else if (allowedCompanies.Count == 0)
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

        return Results.Ok(new ClientLookupResponse { Companies = companies });
    }

    private static async Task<IResult> PreviewClientsCsvAsync(
        HttpContext httpContext,
        ISqlConnectionFactory connectionFactory,
        CancellationToken cancellationToken)
    {
        if (!httpContext.Request.HasFormContentType)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["Request must be multipart/form-data."]
            });
        }

        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");

        if (file is null || file.Length == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["CSV file is required."]
            });
        }

        if (!int.TryParse(form["companyId"], out var companyId) || companyId <= 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["companyId"] = ["companyId is required and must be greater than zero."]
            });
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["CSV file must be up to 5MB."]
            });
        }

        using var connection = connectionFactory.CreateConnection();

        var actorUserId = ResolveActorUserId(httpContext.User);
        var isAdmin = IsAdmin(httpContext.User);
        var companyMap = await connection.QueryFirstOrDefaultAsync<ImportCompanyMapRow>(new CommandDefinition(
            ImportQueries.GetCompanyMapById,
            new { CompanyId = companyId },
            cancellationToken: cancellationToken));

        if (companyMap is null)
        {
            throw new ValidationException("Company is invalid or inactive.");
        }

        if (!isAdmin)
        {
            var hasAccess = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                ImportQueries.CountUserAccessToCompany,
                new { UserId = actorUserId, CompanyId = companyId },
                cancellationToken: cancellationToken));

            if (hasAccess == 0)
            {
                throw new ValidationException("Selected company is not available for the authenticated user.");
            }
        }

        var lines = new List<ImportPreviewLineResponse>();
        var totalRows = 0;

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            throw new ValidationException("CSV header is empty.");
        }

        ValidateHeader(headerLine);

        var lineNumber = 1;
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            totalRows++;

            try
            {
                var parsed = ParseLine(lineNumber, line);
                ValidateLineRules(parsed);

                lines.Add(new ImportPreviewLineResponse
                {
                    LineNumber = parsed.LineNumber,
                    Action = parsed.Action,
                    FirstName = parsed.FirstName,
                    LastName = parsed.LastName,
                    Document = parsed.Document,
                    Email = parsed.Email,
                    Gender = parsed.Gender,
                    BirthDate = parsed.BirthDate,
                    Department = parsed.Department,
                    Role = parsed.Role,
                    IsValid = true
                });
            }
            catch (Exception ex)
            {
                var parsed = TryParseLoosely(lineNumber, line);

                lines.Add(new ImportPreviewLineResponse
                {
                    LineNumber = lineNumber,
                    Action = parsed?.Action ?? string.Empty,
                    FirstName = parsed?.FirstName ?? string.Empty,
                    LastName = parsed?.LastName ?? string.Empty,
                    Document = parsed?.Document ?? string.Empty,
                    Email = parsed?.Email ?? string.Empty,
                    Gender = parsed?.Gender ?? string.Empty,
                    BirthDate = parsed?.BirthDate ?? string.Empty,
                    Department = parsed?.Department ?? string.Empty,
                    Role = parsed?.Role ?? string.Empty,
                    IsValid = false,
                    ValidationMessage = ex.Message
                });
            }
        }

        return Results.Ok(new ImportPreviewCsvResponse
        {
            FileName = file.FileName,
            CompanyId = companyId,
            TotalRows = totalRows,
            ValidRows = lines.Count(l => l.IsValid),
            InvalidRows = lines.Count(l => !l.IsValid),
            Lines = lines
        });
    }

    private static async Task<IResult> ProcessSelectedClientsCsvAsync(
        [FromBody] ImportProcessSelectedRequest request,
        HttpContext httpContext,
        ISqlConnectionFactory connectionFactory,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (request.CompanyId <= 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["companyId"] = ["companyId is required and must be greater than zero."]
            });
        }

        if (request.SelectedLines.Count == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["selectedLines"] = ["At least one line must be selected."]
            });
        }

        var logger = loggerFactory.CreateLogger("ImportCsvClientsSelected");
        var startedAtUtc = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationIdMiddleware.GetCorrelationId(httpContext);

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(ImportQueries.EnsureImportTables, cancellationToken: cancellationToken));

        var actorUserId = ResolveActorUserId(httpContext.User);
        var isAdmin = IsAdmin(httpContext.User);

        var companyMap = await connection.QueryFirstOrDefaultAsync<ImportCompanyMapRow>(new CommandDefinition(
            ImportQueries.GetCompanyMapById,
            new { CompanyId = request.CompanyId },
            cancellationToken: cancellationToken));

        if (companyMap is null)
        {
            throw new ValidationException("Company is invalid or inactive.");
        }

        if (!isAdmin)
        {
            var hasAccess = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                ImportQueries.CountUserAccessToCompany,
                new { UserId = actorUserId, CompanyId = request.CompanyId },
                cancellationToken: cancellationToken));

            if (hasAccess == 0)
            {
                throw new ValidationException("Selected company is not available for the authenticated user.");
            }
        }

        var fileName = string.IsNullOrWhiteSpace(request.FileName) ? "selected-lines.csv" : request.FileName.Trim();

        var jobId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            ImportQueries.InsertImportJob,
            new
            {
                Feature = "clients-csv-selected",
                FileName = fileName,
                CompanyId = request.CompanyId,
                Status = "processing",
                TotalRows = 0,
                SuccessRows = 0,
                ErrorRows = 0,
                DurationMs = 0,
                StartedAtUtc = startedAtUtc,
                FinishedAtUtc = (DateTime?)null,
                CreatedByUserId = actorUserId
            },
            cancellationToken: cancellationToken));

        ImportJobMetrics.MarkStarted();
        logger.LogInformation(
            "Import selected-lines job started. JobId={JobId} CompanyId={CompanyId} ActorUserId={ActorUserId} FileName={FileName} CorrelationId={CorrelationId}",
            jobId,
            request.CompanyId,
            actorUserId,
            fileName,
            correlationId);

        var rowErrors = new List<ImportRowErrorResponse>();
        var successRows = 0;
        var totalRows = 0;

        try
        {
            foreach (var line in request.SelectedLines.OrderBy(l => l.LineNumber))
            {
                totalRows++;

                var data = new CsvImportLineData
                {
                    LineNumber = line.LineNumber,
                    Action = NormalizeAction(line.Action),
                    FirstName = NormalizeText(line.FirstName, 120),
                    LastName = NormalizeText(line.LastName, 120),
                    Document = NormalizeDigits(line.Document, 14),
                    Email = NormalizeEmail(line.Email),
                    Gender = NormalizeGender(line.Gender),
                    BirthDate = NormalizeDate(line.BirthDate),
                    Department = NormalizeText(line.Department, 120),
                    Role = NormalizeText(line.Role, 120)
                };

                try
                {
                    await ProcessLineAsync(connection, companyMap, data, cancellationToken);
                    successRows++;
                }
                catch (Exception ex)
                {
                    var error = new ImportRowErrorResponse
                    {
                        LineNumber = data.LineNumber,
                        Action = data.Action,
                        Document = data.Document,
                        Email = data.Email,
                        Message = ex.Message
                    };

                    rowErrors.Add(error);

                    logger.LogWarning(ex,
                        "Import selected line failed. JobId={JobId} CompanyId={CompanyId} ActorUserId={ActorUserId} Line={Line} Action={Action} Document={Document} Email={Email} CorrelationId={CorrelationId}",
                        jobId,
                        request.CompanyId,
                        actorUserId,
                        data.LineNumber,
                        error.Action,
                        error.Document,
                        error.Email,
                        correlationId);
                }
            }

            stopwatch.Stop();
            var status = rowErrors.Count == 0 ? "completed" : successRows == 0 ? "failed" : "completed_with_errors";

            await connection.ExecuteAsync(new CommandDefinition(
                ImportQueries.UpdateImportJob,
                new
                {
                    Id = jobId,
                    Status = status,
                    TotalRows = totalRows,
                    SuccessRows = successRows,
                    ErrorRows = rowErrors.Count,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    FinishedAtUtc = DateTime.UtcNow
                },
                cancellationToken: cancellationToken));

            foreach (var error in rowErrors)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    ImportQueries.InsertImportJobError,
                    new
                    {
                        ImportJobId = jobId,
                        error.LineNumber,
                        error.Action,
                        error.Document,
                        error.Email,
                        error.Message
                    },
                    cancellationToken: cancellationToken));
            }

            logger.LogInformation(
                "Import selected-lines job finished. JobId={JobId} CompanyId={CompanyId} ActorUserId={ActorUserId} FileName={FileName} Status={Status} TotalRows={TotalRows} SuccessRows={SuccessRows} ErrorRows={ErrorRows} DurationMs={DurationMs} CorrelationId={CorrelationId} Metrics={Metrics}",
                jobId,
                request.CompanyId,
                actorUserId,
                fileName,
                status,
                totalRows,
                successRows,
                rowErrors.Count,
                (int)stopwatch.ElapsedMilliseconds,
                correlationId,
                ImportJobMetrics.Snapshot());

            ImportJobMetrics.MarkCompleted(failed: string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase));

            return Results.Ok(new ImportClientsCsvResponse
            {
                JobId = jobId,
                Status = status,
                FileName = fileName,
                CompanyId = request.CompanyId,
                TotalRows = totalRows,
                SuccessRows = successRows,
                ErrorRows = rowErrors.Count,
                DurationMs = (int)stopwatch.ElapsedMilliseconds,
                Errors = rowErrors
            });
        }
        catch
        {
            stopwatch.Stop();

            ImportJobMetrics.MarkCompleted(failed: true);

            await connection.ExecuteAsync(new CommandDefinition(
                ImportQueries.UpdateImportJob,
                new
                {
                    Id = jobId,
                    Status = "failed",
                    TotalRows = totalRows,
                    SuccessRows = successRows,
                    ErrorRows = rowErrors.Count,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    FinishedAtUtc = DateTime.UtcNow
                },
                cancellationToken: cancellationToken));

            throw;
        }
    }

    private static async Task<IResult> ListJobsAsync(
        [AsParameters] ImportJobListRequest request,
        ISqlConnectionFactory connectionFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (request.Page <= 0 || request.PageSize is < 1 or > 100)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["page"] = ["Page must be greater than zero and pageSize between 1 and 100."]
            });
        }

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(ImportQueries.EnsureImportTables, cancellationToken: cancellationToken));

        var isAdmin = IsAdmin(httpContext.User);
        var actorUserId = ResolveActorUserId(httpContext.User);
        var offset = (request.Page - 1) * request.PageSize;

        var listSql = isAdmin
            ? ImportQueries.ListImportJobs
            : ImportQueries.ListImportJobs.Replace("ORDER BY j.id DESC", "AND j.created_by_user_id = @CreatedByUserId ORDER BY j.id DESC");

        var countSql = isAdmin
            ? ImportQueries.CountImportJobs
            : ImportQueries.CountImportJobs + " WHERE created_by_user_id = @CreatedByUserId";

        var items = (await connection.QueryAsync<ImportJobRow>(new CommandDefinition(
            listSql,
            new { Offset = offset, PageSize = request.PageSize, CreatedByUserId = actorUserId },
            cancellationToken: cancellationToken))).ToArray();

        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            countSql,
            new { CreatedByUserId = actorUserId },
            cancellationToken: cancellationToken));

        return Results.Ok(new ImportJobListResponse
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total,
            Items = items.Select(ToJobItem).ToArray()
        });
    }

    private static async Task<IResult> GetJobByIdAsync(
        [FromRoute] int id,
        ISqlConnectionFactory connectionFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(ImportQueries.EnsureImportTables, cancellationToken: cancellationToken));

        var job = await connection.QueryFirstOrDefaultAsync<ImportJobRow>(new CommandDefinition(
            ImportQueries.GetImportJobById,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (job is null)
        {
            return Results.NotFound();
        }

        var isAdmin = IsAdmin(httpContext.User);
        var actorUserId = ResolveActorUserId(httpContext.User);

        if (!isAdmin && job.CreatedByUserId != actorUserId)
        {
            return Results.NotFound();
        }

        var errors = (await connection.QueryAsync<ImportJobErrorRow>(new CommandDefinition(
            ImportQueries.GetImportErrorsByJobId,
            new { ImportJobId = id },
            cancellationToken: cancellationToken))).ToArray();

        return Results.Ok(new ImportJobDetailResponse
        {
            Id = job.Id,
            Feature = job.Feature,
            FileName = job.FileName,
            CompanyId = job.CompanyId,
            Status = job.Status,
            TotalRows = job.TotalRows,
            SuccessRows = job.SuccessRows,
            ErrorRows = job.ErrorRows,
            DurationMs = job.DurationMs,
            StartedAtUtc = job.StartedAtUtc,
            FinishedAtUtc = job.FinishedAtUtc,
            CreatedByUserId = job.CreatedByUserId,
            Errors = errors.Select(e => new ImportRowErrorResponse
            {
                LineNumber = e.LineNumber,
                Action = e.Action,
                Document = e.Document,
                Email = e.Email,
                Message = e.Message
            }).ToArray()
        });
    }

    private static void ValidateHeader(string headerLine)
    {
        var columns = headerLine.Split(';').Select(v => v.Trim().ToLowerInvariant()).ToArray();
        if (columns.Length != ExpectedColumns.Length)
        {
            throw new ValidationException("CSV header has invalid number of columns.");
        }

        for (var i = 0; i < ExpectedColumns.Length; i++)
        {
            if (!string.Equals(columns[i], ExpectedColumns[i], StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException($"CSV header mismatch at column {i + 1}. Expected '{ExpectedColumns[i]}'.");
            }
        }
    }

    private static CsvImportLineData ParseLine(int lineNumber, string line)
    {
        var parts = line.Split(';');
        if (parts.Length != ExpectedColumns.Length)
        {
            throw new ValidationException("Line must contain exactly 9 columns separated by ';'.");
        }

        return new CsvImportLineData
        {
            LineNumber = lineNumber,
            RawLine = line,
            FirstName = NormalizeText(parts[0], 120),
            LastName = NormalizeText(parts[1], 120),
            Document = NormalizeDigits(parts[2], 14),
            Email = NormalizeEmail(parts[3]),
            Gender = NormalizeGender(parts[4]),
            BirthDate = NormalizeDate(parts[5]),
            Department = NormalizeText(parts[6], 120),
            Role = NormalizeText(parts[7], 120),
            Action = NormalizeAction(parts[8])
        };
    }

    private static CsvImportLineData? TryParseLoosely(int lineNumber, string line)
    {
        var parts = line.Split(';');
        if (parts.Length == 0)
        {
            return null;
        }

        string Safe(int index) => parts.Length > index ? parts[index] : string.Empty;

        return new CsvImportLineData
        {
            LineNumber = lineNumber,
            RawLine = line,
            Action = Safe(8).Trim(),
            Document = NormalizeDigits(Safe(2), 14),
            Email = Safe(3).Trim()
        };
    }

    private static async Task ProcessLineAsync(
        System.Data.IDbConnection connection,
        ImportCompanyMapRow company,
        CsvImportLineData data,
        CancellationToken cancellationToken)
    {
        ValidateLineRules(data);

        var existing = await connection.QueryFirstOrDefaultAsync<ImportExistingClientRow>(new CommandDefinition(
            ImportQueries.FindActiveClientByDocumentOrEmail,
            new
            {
                company.PartnerId,
                data.Document,
                data.Email
            },
            cancellationToken: cancellationToken));

        if (data.Action == "inserir")
        {
            if (existing is not null)
            {
                throw new ValidationException("Client already exists for action 'inserir'.");
            }

            await connection.ExecuteAsync(new CommandDefinition(
                ImportQueries.InsertClient,
                new
                {
                    data.FirstName,
                    data.LastName,
                    DocumentHash = ComputeMd5Hex(data.Document),
                    data.Email,
                    Registration = BuildRegistration(data),
                    company.PartnerId,
                    ClientGuid = Guid.NewGuid()
                },
                cancellationToken: cancellationToken));

            return;
        }

        if (existing is null)
        {
            throw new ValidationException($"Client not found for action '{data.Action}'.");
        }

        if (data.Action == "atualizar")
        {
            await connection.ExecuteAsync(new CommandDefinition(
                ImportQueries.UpdateClient,
                new
                {
                    existing.Id,
                    data.FirstName,
                    data.LastName,
                    DocumentHash = ComputeMd5Hex(data.Document),
                    data.Email,
                    Registration = BuildRegistration(data),
                    company.PartnerId
                },
                cancellationToken: cancellationToken));

            return;
        }

        if (data.Action == "excluir")
        {
            await connection.ExecuteAsync(new CommandDefinition(
                ImportQueries.InactivateClient,
                new { existing.Id },
                cancellationToken: cancellationToken));

            return;
        }

        throw new ValidationException("Unsupported action.");
    }

    private static void ValidateLineRules(CsvImportLineData data)
    {
        if (string.IsNullOrWhiteSpace(data.FirstName))
        {
            throw new ValidationException("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(data.LastName))
        {
            throw new ValidationException("Last name is required.");
        }

        if (data.Document.Length is not 11 and not 14)
        {
            throw new ValidationException("Document must contain 11 or 14 digits.");
        }

        if (!string.IsNullOrWhiteSpace(data.Email) && !Regex.IsMatch(data.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase))
        {
            throw new ValidationException("E-mail is invalid.");
        }

        if (data.Action is not "inserir" and not "atualizar" and not "excluir")
        {
            throw new ValidationException("Action must be inserir, atualizar or excluir.");
        }
    }

    private static string NormalizeText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            normalized = normalized[..maxLength];
        }

        return normalized;
    }

    private static string NormalizeDigits(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length > maxLength)
        {
            digits = digits[..maxLength];
        }

        return digits;
    }

    private static string NormalizeEmail(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    private static string NormalizeAction(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    private static string NormalizeGender(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "not-to-say";
        }

        var letter = value.Trim().Substring(0, 1).ToUpperInvariant();

        return letter switch
        {
            "M" => "male",
            "F" => "female",
            _ => "not-to-say"
        };
    }

    private static string NormalizeDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var text = value.Trim();

        if (DateTime.TryParseExact(text, ["dd/MM/yyyy", "yyyy-MM-dd"],
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var date))
        {
            return date.ToString("yyyy-MM-dd");
        }

        throw new ValidationException("Birth date is invalid. Use dd/MM/yyyy.");
    }

    private static string BuildRegistration(CsvImportLineData data)
    {
        var candidate = !string.IsNullOrWhiteSpace(data.Role)
            ? data.Role
            : data.Department;

        // Ambientes legados possuem variação de tamanho para registro_colaborador
        // (inclusive comprimento 1). Limite defensivo para evitar truncation error.
        return NormalizeText(candidate, 1);
    }

    private static string ComputeMd5Hex(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        var hash = MD5.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static ImportJobItemResponse ToJobItem(ImportJobRow row) => new()
    {
        Id = row.Id,
        Feature = row.Feature,
        FileName = row.FileName,
        CompanyId = row.CompanyId,
        Status = row.Status,
        TotalRows = row.TotalRows,
        SuccessRows = row.SuccessRows,
        ErrorRows = row.ErrorRows,
        DurationMs = row.DurationMs,
        StartedAtUtc = row.StartedAtUtc,
        FinishedAtUtc = row.FinishedAtUtc,
        CreatedByUserId = row.CreatedByUserId
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
