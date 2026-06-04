using Dapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Partner.Api.Features.Clients;
using Partner.Api.Infrastructure.Database;
using Partner.Api.Infrastructure.Import;
using Partner.Api.Infrastructure.Observability;
using Partner.Api.Infrastructure.Security;
using Partner.Api.Middleware;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
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

        group.MapPost("/{feature}/csv/async", ImportAsyncCsvAsync)
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ImportAsyncUploadResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/clients-csv/preview", PreviewClientsCsvAsync)
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ImportPreviewCsvResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/clients-csv/process-selected", ProcessSelectedClientsCsvAsync)
            .Accepts<ImportProcessSelectedRequest>("application/json")
            .Produces<ImportAsyncUploadResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/lookups", GetLookupsAsync)
            .Produces<ClientLookupResponse>(StatusCodes.Status200OK);

        group.MapGet("/jobs", ListJobsAsync)
            .Produces<ImportJobListResponse>(StatusCodes.Status200OK);

        group.MapGet("/jobs/{id:int}", GetJobByIdAsync)
            .Produces<ImportJobDetailResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/jobs/{jobPublicId:guid}", GetJobByPublicIdAsync)
            .Produces<ImportJobDetailResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/jobs/{jobPublicId:guid}/errors", GetJobErrorsAsync)
            .Produces<ImportJobErrorsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/jobs/{jobPublicId:guid}/errors/export", ExportJobErrorsAsync)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/jobs/{jobPublicId:guid}/cancel", CancelImportJobAsync)
            .Produces<ImportJobActionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/jobs/{jobPublicId:guid}/retry", RetryImportJobAsync)
            .Produces<ImportJobActionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/notifications", ListNotificationsAsync)
            .Produces<IReadOnlyCollection<ImportNotificationResponse>>(StatusCodes.Status200OK);

        group.MapPost("/notifications/{id:int}/read", MarkNotificationReadAsync)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/notifications/read-all", MarkAllNotificationsReadAsync)
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }

    private static async Task<IResult> ListNotificationsAsync(
        ISqlConnectionFactory connectionFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(ImportQueries.EnsureImportTables, cancellationToken: cancellationToken));

        var actorUserId = ResolveActorUserId(httpContext.User);

        var items = (await connection.QueryAsync<ImportNotificationRow>(new CommandDefinition(
            ImportQueries.ListImportNotifications,
            new { UserId = actorUserId },
            cancellationToken: cancellationToken))).ToArray();

        var response = items.Select(item => new ImportNotificationResponse
        {
            Id = item.Id,
            Title = item.Title,
            Message = item.Message,
            Status = item.Status,
            CreatedAtUtc = item.CreatedAtUtc,
            ImportJobPublicId = item.ImportJobPublicId
        }).ToArray();

        return Results.Ok(response);
    }

    private static async Task<IResult> MarkNotificationReadAsync(
        [FromRoute] int id,
        ISqlConnectionFactory connectionFactory,
        HttpContext httpContext,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(ImportQueries.EnsureImportTables, cancellationToken: cancellationToken));

        var actorUserId = ResolveActorUserId(httpContext.User);
        var correlationId = CorrelationIdMiddleware.GetCorrelationId(httpContext);

        var notification = await connection.QueryFirstOrDefaultAsync<ImportNotificationRow>(new CommandDefinition(
            ImportQueries.GetImportNotificationById,
            new { Id = id, UserId = actorUserId },
            cancellationToken: cancellationToken));

        if (notification is null)
        {
            return Results.NotFound();
        }

        var updated = await connection.ExecuteAsync(new CommandDefinition(
            ImportQueries.MarkImportNotificationRead,
            new
            {
                Id = id,
                UserId = actorUserId,
                Status = "Read",
                ReadAtUtc = DateTime.UtcNow,
                UnreadStatus = "Unread"
            },
            cancellationToken: cancellationToken));

        if (updated > 0)
        {
            var logger = loggerFactory.CreateLogger("ImportNotifications");
            logger.LogInformation(
                "NotificationRead NotificationId={NotificationId} JobId={JobId} JobPublicId={JobPublicId} UserId={UserId} CorrelationId={CorrelationId}",
                notification.Id,
                notification.ImportJobId,
                notification.ImportJobPublicId,
                actorUserId,
                correlationId);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> MarkAllNotificationsReadAsync(
        ISqlConnectionFactory connectionFactory,
        HttpContext httpContext,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(ImportQueries.EnsureImportTables, cancellationToken: cancellationToken));

        var actorUserId = ResolveActorUserId(httpContext.User);
        var correlationId = CorrelationIdMiddleware.GetCorrelationId(httpContext);

        var updated = await connection.ExecuteAsync(new CommandDefinition(
            ImportQueries.MarkAllImportNotificationsRead,
            new
            {
                UserId = actorUserId,
                Status = "Read",
                ReadAtUtc = DateTime.UtcNow,
                UnreadStatus = "Unread"
            },
            cancellationToken: cancellationToken));

        var logger = loggerFactory.CreateLogger("ImportNotifications");
        logger.LogInformation(
            "NotificationReadAll UserId={UserId} Count={Count} CorrelationId={CorrelationId}",
            actorUserId,
            updated,
            correlationId);

        return Results.NoContent();
    }

    private static async Task<IResult> ImportAsyncCsvAsync(
        HttpContext httpContext,
        string feature,
        ISqlConnectionFactory connectionFactory,
        ImportRabbitMqConnectionFactory rabbitMqConnectionFactory,
        IOptions<ImportRabbitMqOptions> rabbitMqOptions,
        IOptions<ImportStorageOptions> storageOptions,
        IHostEnvironment hostEnvironment,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("ImportCsvAsync");
        var startedAtUtc = DateTime.UtcNow;
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

        var publicId = Guid.NewGuid();
        var storageRoot = storageOptions.Value.StorageRoot;
        var relativePath = Path.Combine(storageRoot, companyId.ToString(), publicId.ToString(), "source.csv");
        var fullPath = Path.Combine(hostEnvironment.ContentRootPath, relativePath);

        var fileHash = await ImportFileStorage.SaveStreamAndComputeHashAsync(file.OpenReadStream(), fullPath, cancellationToken);

        var existingJob = await connection.QueryFirstOrDefaultAsync<ImportJobIdempotencyRow>(
            new CommandDefinition(ImportQueries.FindJobByIdempotencyKey,
            new { CompanyId = companyId, Feature = feature, FileHashSha256 = fileHash },
            cancellationToken: cancellationToken));

        if (existingJob is not null
            && !ImportJobStatus.IsTerminal(existingJob.Status)
            && DateTime.UtcNow.Subtract(existingJob.StartedAtUtc) <= TimeSpan.FromMinutes(10))
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            logger.LogInformation(
                "Import job idempotency hit. ExistingJobId={JobId} PublicId={PublicId} CompanyId={CompanyId} Feature={Feature} CorrelationId={CorrelationId}",
                existingJob.Id,
                existingJob.PublicId,
                companyId,
                feature,
                correlationId);

            return Results.Ok(new ImportAsyncUploadResponse
            {
                JobPublicId = existingJob.PublicId,
                Status = existingJob.Status
            });
        }

        var jobRequest = ImportJobRequestFactory.CreateQueued(
            publicId,
            feature,
            file.FileName,
            relativePath,
            fileHash,
            companyId,
            actorUserId,
            startedAtUtc,
            correlationId);

        var jobId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            ImportQueries.InsertImportJob,
            jobRequest,
            cancellationToken: cancellationToken));

        PublishImportJob(rabbitMqConnectionFactory, rabbitMqOptions.Value, correlationId, jobId, jobRequest);

        logger.LogInformation(
            "ImportJobQueued JobId={JobId} JobPublicId={JobPublicId} CompanyId={CompanyId} Feature={Feature} CorrelationId={CorrelationId}",
            jobId,
            publicId,
            companyId,
            feature,
            correlationId);

        return Results.Ok(new ImportAsyncUploadResponse
        {
            JobPublicId = publicId,
            Status = ImportJobStatus.Queued
        });
    }

    private static void PublishImportJob(
        ImportRabbitMqConnectionFactory connectionFactory,
        ImportRabbitMqOptions options,
        string correlationId,
        int jobId,
        Partner.Api.Features.Import.ImportJobCreateRequest request)
    {
        var message = ImportJobMessageFactory.Create(jobId, request, correlationId);
        ImportJobPublisher.Publish(connectionFactory, options, message);
    }

    private static async Task<IResult> ImportClientsCsvAsync(
        HttpContext httpContext,
        ISqlConnectionFactory connectionFactory,
        ICpfProtectionService cpfService,
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
                PublicId = Guid.NewGuid(),
                Feature = "clients-csv",
                FileName = file.FileName,
                FilePath = file.FileName,
                FileHashSha256 = "legacy",
                CompanyId = companyId,
                Status = ImportJobStatus.Running,
                TotalRows = 0,
                ProcessedRows = 0,
                SuccessRows = 0,
                ErrorRows = 0,
                DurationMs = 0,
                StartedAtUtc = startedAtUtc,
                CreatedAtUtc = startedAtUtc,
                FinishedAtUtc = (DateTime?)null,
                CreatedByUserId = actorUserId,
                CancelRequested = false,
                CancelRequestedAtUtc = (DateTime?)null,
                Attempts = 0,
                LastError = (string?)null,
                LockedBy = (string?)null,
                LockedAtUtc = (DateTime?)null,
                LastHeartbeatAtUtc = (DateTime?)null,
                CorrelationId = correlationId
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
                    await ProcessLineAsync(connection, companyMap, data, cpfService, cancellationToken);
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
                        cpfService.Mask(error.Document ?? string.Empty),
                        error.Email,
                        correlationId);
                }
            }

            stopwatch.Stop();
            var status = rowErrors.Count == 0
                ? ImportJobStatus.Completed
                : successRows == 0
                    ? ImportJobStatus.Failed
                    : ImportJobStatus.CompletedWithErrors;

            await connection.ExecuteAsync(new CommandDefinition(
                ImportQueries.UpdateImportJob,
                new
                {
                    Id = jobId,
                    Status = status,
                    TotalRows = totalRows,
                    ProcessedRows = totalRows,
                    SuccessRows = successRows,
                    ErrorRows = rowErrors.Count,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    FinishedAtUtc = DateTime.UtcNow,
                    Attempts = 0,
                    LastError = (string?)null,
                    LockedBy = (string?)null,
                    LockedAtUtc = (DateTime?)null,
                    LastHeartbeatAtUtc = (DateTime?)null
                },
                cancellationToken: cancellationToken));

            var notification = BuildNotification(status, file.FileName);
            if (notification is not null)
            {
                var notificationId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    ImportQueries.InsertImportNotification,
                    new
                    {
                        ImportJobId = jobId,
                        UserId = actorUserId,
                        Title = notification.Value.Title,
                        Message = notification.Value.Message,
                        Status = "Unread",
                        CreatedAtUtc = DateTime.UtcNow,
                        ReadAtUtc = (DateTime?)null
                    },
                    cancellationToken: cancellationToken));

                logger.LogInformation(
                    "NotificationCreated NotificationId={NotificationId} JobId={JobId} JobPublicId={JobPublicId} UserId={UserId} Status={Status} CorrelationId={CorrelationId}",
                    notificationId,
                    jobId,
                    Guid.Empty,
                    actorUserId,
                    status,
                    correlationId);
            }

            foreach (var error in rowErrors)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    ImportQueries.InsertImportJobError,
                    new
                    {
                        ImportJobId = jobId,
                        Seq = error.LineNumber,
                        LineNumber = error.LineNumber,
                        ErrorCode = (string?)null,
                        error.Message,
                        RawLine = (string?)null,
                        error.Action,
                        error.Document,
                        error.Email,
                        CreatedAtUtc = DateTime.UtcNow
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

            ImportJobMetrics.MarkCompleted(failed: string.Equals(status, ImportJobStatus.Failed, StringComparison.OrdinalIgnoreCase));

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
                    Status = ImportJobStatus.Failed,
                    TotalRows = totalRows,
                    ProcessedRows = totalRows,
                    SuccessRows = successRows,
                    ErrorRows = rowErrors.Count,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    FinishedAtUtc = DateTime.UtcNow,
                    Attempts = 0,
                    LastError = (string?)null,
                    LockedBy = (string?)null,
                    LockedAtUtc = (DateTime?)null,
                    LastHeartbeatAtUtc = (DateTime?)null
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
        ICpfProtectionService cpfService,
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
                ValidateLineRules(parsed, cpfService);

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
        ImportRabbitMqConnectionFactory rabbitMqConnectionFactory,
        IOptions<ImportRabbitMqOptions> rabbitMqOptions,
        IOptions<ImportStorageOptions> storageOptions,
        IHostEnvironment hostEnvironment,
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
        var publicId = Guid.NewGuid();
        var storageRoot = storageOptions.Value.StorageRoot;
        var relativePath = Path.Combine(storageRoot, request.CompanyId.ToString(), publicId.ToString(), "selected-lines.json");
        var fullPath = Path.Combine(hostEnvironment.ContentRootPath, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, JsonSerializer.Serialize(request.SelectedLines), cancellationToken);

        var fileHash = ImportFileStorage.ComputeSha256(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request.SelectedLines)));

        var jobRequest = ImportJobRequestFactory.CreateQueued(
            publicId,
            "clients-csv-selected",
            fileName,
            relativePath,
            fileHash,
            request.CompanyId,
            actorUserId,
            startedAtUtc,
            correlationId);

        var jobId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            ImportQueries.InsertImportJob,
            jobRequest,
            cancellationToken: cancellationToken));

        PublishImportJob(rabbitMqConnectionFactory, rabbitMqOptions.Value, correlationId, jobId, jobRequest);

        logger.LogInformation(
            "ImportJobQueued JobId={JobId} JobPublicId={JobPublicId} CompanyId={CompanyId} Feature={Feature} CorrelationId={CorrelationId}",
            jobId,
            publicId,
            request.CompanyId,
            jobRequest.Feature,
            correlationId);

        return Results.Ok(new ImportAsyncUploadResponse
        {
            JobPublicId = publicId,
            Status = ImportJobStatus.Queued
        });
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

        var isAdmin = IsAdmin(httpContext.User);
        var actorUserId = ResolveActorUserId(httpContext.User);
        var targetUserId = isAdmin ? request.UserId : actorUserId;
        var startDateUtc = request.StartDateUtc;
        var endDateUtc = request.EndDateUtc;
        if (startDateUtc.HasValue && endDateUtc.HasValue && endDateUtc < startDateUtc)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["dateRange"] = ["EndDateUtc must be greater than StartDateUtc."]
            });
        }

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(ImportQueries.EnsureImportTables, cancellationToken: cancellationToken));

        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();
        var offset = (request.Page - 1) * request.PageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", request.PageSize);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            whereClauses.Add("j.status = @Status");
            parameters.Add("Status", request.Status.Trim());
        }

        if (startDateUtc.HasValue)
        {
            whereClauses.Add("j.created_at_utc >= @StartDateUtc");
            parameters.Add("StartDateUtc", startDateUtc.Value);
        }

        if (endDateUtc.HasValue)
        {
            whereClauses.Add("j.created_at_utc <= @EndDateUtc");
            parameters.Add("EndDateUtc", endDateUtc.Value);
        }

        if (targetUserId.HasValue)
        {
            whereClauses.Add("j.created_by_user_id = @CreatedByUserId");
            parameters.Add("CreatedByUserId", targetUserId.Value);
        }

        var whereClause = whereClauses.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", whereClauses);
        var listSql = ImportQueries.ListImportJobs.Replace("/**where**/", whereClause);
        var countSql = ImportQueries.CountImportJobs.Replace("/**where**/", whereClause);

        var items = (await connection.QueryAsync<ImportJobRow>(new CommandDefinition(
            listSql,
            parameters,
            cancellationToken: cancellationToken))).ToArray();

        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            countSql,
            parameters,
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
        ICpfProtectionService cpfService,
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
            JobPublicId = job.JobPublicId,
            Feature = job.Feature,
            FileName = job.FileName,
            CompanyId = job.CompanyId,
            Status = job.Status,
            TotalRows = job.TotalRows,
            ProcessedRows = job.ProcessedRows,
            SuccessRows = job.SuccessRows,
            ErrorRows = job.ErrorRows,
            ProgressPercent = CalculateProgressPercent(job.ProcessedRows, job.TotalRows),
            DurationMs = job.DurationMs,
            CreatedAtUtc = job.CreatedAtUtc,
            StartedAtUtc = job.StartedAtUtc,
            FinishedAtUtc = job.FinishedAtUtc,
            CreatedByUserId = job.CreatedByUserId,
            Errors = errors.Select(e => new ImportRowErrorResponse
            {
                LineNumber = e.LineNumber,
                Field = ResolveErrorField(e),
                Action = e.Action,
                Document = cpfService.Mask(e.Document ?? string.Empty),
                Email = e.Email,
                Message = e.Message
            }).ToArray()
        });
    }

    private static async Task<IResult> GetJobByPublicIdAsync(
        [FromRoute] Guid jobPublicId,
        ISqlConnectionFactory connectionFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(ImportQueries.EnsureImportTables, cancellationToken: cancellationToken));

        var job = await connection.QueryFirstOrDefaultAsync<ImportJobRow>(new CommandDefinition(
            ImportQueries.GetImportJobByPublicId,
            new { PublicId = jobPublicId },
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

        return Results.Ok(new ImportJobDetailResponse
        {
            Id = job.Id,
            JobPublicId = job.JobPublicId,
            Feature = job.Feature,
            FileName = job.FileName,
            CompanyId = job.CompanyId,
            Status = job.Status,
            TotalRows = job.TotalRows,
            ProcessedRows = job.ProcessedRows,
            SuccessRows = job.SuccessRows,
            ErrorRows = job.ErrorRows,
            ProgressPercent = CalculateProgressPercent(job.ProcessedRows, job.TotalRows),
            DurationMs = job.DurationMs,
            CreatedAtUtc = job.CreatedAtUtc,
            StartedAtUtc = job.StartedAtUtc,
            FinishedAtUtc = job.FinishedAtUtc,
            CreatedByUserId = job.CreatedByUserId,
            Errors = []
        });
    }

    private static async Task<IResult> GetJobErrorsAsync(
        [FromRoute] Guid jobPublicId,
        [AsParameters] ImportJobErrorsRequest request,
        ISqlConnectionFactory connectionFactory,
        ICpfProtectionService cpfService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (request.Page <= 0 || request.PageSize is < 1 or > 200)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["page"] = ["Page must be greater than zero and pageSize between 1 and 200."]
            });
        }

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(ImportQueries.EnsureImportTables, cancellationToken: cancellationToken));

        var job = await connection.QueryFirstOrDefaultAsync<ImportJobRow>(new CommandDefinition(
            ImportQueries.GetImportJobByPublicId,
            new { PublicId = jobPublicId },
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

        var offset = (request.Page - 1) * request.PageSize;
        var parameters = new { ImportJobId = job.Id, Offset = offset, PageSize = request.PageSize };

        var items = (await connection.QueryAsync<ImportJobErrorRow>(new CommandDefinition(
            ImportQueries.ListImportErrors,
            parameters,
            cancellationToken: cancellationToken))).ToArray();

        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            ImportQueries.CountImportErrors,
            new { ImportJobId = job.Id },
            cancellationToken: cancellationToken));

        return Results.Ok(new ImportJobErrorsResponse
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total,
            Items = items.Select(e => new ImportRowErrorResponse
            {
                LineNumber = e.LineNumber,
                Field = ResolveErrorField(e),
                Action = e.Action,
                Document = cpfService.Mask(e.Document ?? string.Empty),
                Email = e.Email,
                Message = e.Message
            }).ToArray()
        });
    }

    private static async Task<IResult> ExportJobErrorsAsync(
        [FromRoute] Guid jobPublicId,
        ISqlConnectionFactory connectionFactory,
        HttpContext httpContext,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(ImportQueries.EnsureImportTables, cancellationToken: cancellationToken));

        var job = await connection.QueryFirstOrDefaultAsync<ImportJobRow>(new CommandDefinition(
            ImportQueries.GetImportJobByPublicId,
            new { PublicId = jobPublicId },
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

        // Buscar todos os erros (sem paginação)
        var errors = (await connection.QueryAsync<ImportJobErrorRow>(new CommandDefinition(
            ImportQueries.GetAllImportErrorsByJobId,
            new { ImportJobId = job.Id },
            cancellationToken: cancellationToken))).ToArray();

        if (errors.Length == 0)
        {
            // Retornar CSV vazio com só cabeçalho
            var emptyContent = Encoding.UTF8.GetBytes("Linha;Ação;CPF;Email;Erro\r\n");
            var correlationId = CorrelationIdMiddleware.GetCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger("ImportErrorsExport");
            logger.LogInformation(
                "ImportErrorsExported CorrelationId={CorrelationId} JobId={JobId} JobPublicId={JobPublicId} UserId={UserId} ErrorCount={ErrorCount}",
                correlationId,
                job.Id,
                jobPublicId,
                actorUserId,
                0);

            return Results.File(emptyContent, "text/csv", $"erros_{job.FileName}_{jobPublicId:N}.csv");
        }

        // Gerar CSV com os erros
        var csv = new StringBuilder();
        csv.AppendLine("Linha;Ação;CPF;Email;Erro");

        foreach (var error in errors)
        {
            var linha = error.LineNumber;
            var acao = error.Action ?? string.Empty;
            var cpf = error.Document ?? string.Empty;
            var email = error.Email ?? string.Empty;
            var erro = error.Message ?? string.Empty;

            // Escapar aspas duplas nos campos
            acao = EscapeCsvField(acao);
            cpf = EscapeCsvField(cpf);
            email = EscapeCsvField(email);
            erro = EscapeCsvField(erro);

            csv.AppendLine($"{linha};{acao};{cpf};{email};{erro}");
        }

        var content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        
        var corId = CorrelationIdMiddleware.GetCorrelationId(httpContext);
        var log = loggerFactory.CreateLogger("ImportErrorsExport");
        log.LogInformation(
            "ImportErrorsExported CorrelationId={CorrelationId} JobId={JobId} JobPublicId={JobPublicId} UserId={UserId} ErrorCount={ErrorCount}",
            corId,
            job.Id,
            jobPublicId,
            actorUserId,
            errors.Length);

        return Results.File(content, "text/csv; charset=utf-8", $"erros_{job.FileName}_{jobPublicId:N}.csv");
    }

    private static async Task<IResult> CancelImportJobAsync(
        [FromRoute] Guid jobPublicId,
        ISqlConnectionFactory connectionFactory,
        HttpContext httpContext,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(ImportQueries.EnsureImportTables, cancellationToken: cancellationToken));

        var job = await connection.QueryFirstOrDefaultAsync<ImportJobRow>(new CommandDefinition(
            ImportQueries.GetImportJobByPublicId,
            new { PublicId = jobPublicId },
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

        if (!string.Equals(job.Status, ImportJobStatus.Queued, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(job.Status, ImportJobStatus.Running, StringComparison.OrdinalIgnoreCase))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = [$"Não é possível cancelar um job no status '{job.Status}'."]
            });
        }

        var updated = await connection.ExecuteAsync(new CommandDefinition(
            ImportQueries.RequestImportJobCancellation,
            new
            {
                Id = job.Id,
                Status = ImportJobStatus.CancellationRequested,
                CancelRequested = true,
                CancelRequestedAtUtc = DateTime.UtcNow,
                AllowedStatuses = new[] { ImportJobStatus.Queued, ImportJobStatus.Running }
            },
            cancellationToken: cancellationToken));

        if (updated == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = ["Não foi possível solicitar o cancelamento. Atualize a tela e tente novamente."]
            });
        }

        var correlationId = CorrelationIdMiddleware.GetCorrelationId(httpContext);
        var logger = loggerFactory.CreateLogger("ImportCancel");
        logger.LogInformation(
            "ImportCancelRequested JobId={JobId} JobPublicId={JobPublicId} UserId={UserId} CorrelationId={CorrelationId}",
            job.Id,
            job.JobPublicId,
            actorUserId,
            correlationId);

        return Results.Ok(new ImportJobActionResponse
        {
            JobPublicId = job.JobPublicId,
            Status = ImportJobStatus.CancellationRequested,
            Message = "Cancelamento solicitado. A importação será interrompida em instantes."
        });
    }

    private static async Task<IResult> RetryImportJobAsync(
        [FromRoute] Guid jobPublicId,
        ISqlConnectionFactory connectionFactory,
        ImportRabbitMqConnectionFactory rabbitMqConnectionFactory,
        IOptions<ImportRabbitMqOptions> rabbitMqOptions,
        HttpContext httpContext,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(ImportQueries.EnsureImportTables, cancellationToken: cancellationToken));

        var job = await connection.QueryFirstOrDefaultAsync<ImportJobRow>(new CommandDefinition(
            ImportQueries.GetImportJobByPublicIdDetailed,
            new { PublicId = jobPublicId },
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

        if (!string.Equals(job.Status, ImportJobStatus.Failed, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(job.Status, ImportJobStatus.CompletedWithErrors, StringComparison.OrdinalIgnoreCase))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = [$"Não é possível reprocessar um job no status '{job.Status}'."]
            });
        }

        var correlationId = CorrelationIdMiddleware.GetCorrelationId(httpContext);
        var logger = loggerFactory.CreateLogger("ImportRetry");
        logger.LogInformation(
            "ImportRetryRequested JobId={JobId} JobPublicId={JobPublicId} UserId={UserId} CorrelationId={CorrelationId}",
            job.Id,
            job.JobPublicId,
            actorUserId,
            correlationId);

        var newPublicId = Guid.NewGuid();
        var jobRequest = ImportJobRequestFactory.CreateQueued(
            newPublicId,
            job.Feature,
            job.FileName,
            job.FilePath,
            job.FileHashSha256,
            job.CompanyId,
            actorUserId,
            DateTime.UtcNow,
            correlationId,
            retryOfImportJobId: job.Id);

        var newJobId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            ImportQueries.InsertImportJob,
            jobRequest,
            cancellationToken: cancellationToken));

        PublishImportJob(rabbitMqConnectionFactory, rabbitMqOptions.Value, correlationId, newJobId, jobRequest);

        logger.LogInformation(
            "ImportRetryCreated JobId={JobId} JobPublicId={JobPublicId} RetryOfJobId={RetryOfJobId} RetryOfJobPublicId={RetryOfJobPublicId} UserId={UserId} CorrelationId={CorrelationId}",
            newJobId,
            newPublicId,
            job.Id,
            job.JobPublicId,
            actorUserId,
            correlationId);

        var notificationId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            ImportQueries.InsertImportNotification,
            new
            {
                ImportJobId = newJobId,
                UserId = actorUserId,
                Title = "Importação reprocessada",
                Message = "Uma nova execução da importação foi criada.",
                Status = "Unread",
                CreatedAtUtc = DateTime.UtcNow,
                ReadAtUtc = (DateTime?)null
            },
            cancellationToken: cancellationToken));

        logger.LogInformation(
            "NotificationCreated NotificationId={NotificationId} JobId={JobId} JobPublicId={JobPublicId} UserId={UserId} Status={Status} CorrelationId={CorrelationId}",
            notificationId,
            newJobId,
            newPublicId,
            actorUserId,
            ImportJobStatus.Queued,
            correlationId);

        return Results.Ok(new ImportJobActionResponse
        {
            JobPublicId = job.JobPublicId,
            Status = ImportJobStatus.Queued,
            Message = "Uma nova execução da importação foi criada.",
            NewJobPublicId = newPublicId
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
            Document = NormalizeDigits(parts[2], 11),
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
            Document = NormalizeDigits(Safe(2), 11),
            Email = Safe(3).Trim()
        };
    }

    private static async Task ProcessLineAsync(
        System.Data.IDbConnection connection,
        ImportCompanyMapRow company,
        CsvImportLineData data,
        ICpfProtectionService cpfService,
        CancellationToken cancellationToken)
    {
        ValidateLineRules(data, cpfService);

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
                    data.Document,
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
                    data.Document,
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

    private static void ValidateLineRules(CsvImportLineData data, ICpfProtectionService cpfService)
    {
        if (string.IsNullOrWhiteSpace(data.FirstName))
        {
            throw new ValidationException("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(data.LastName))
        {
            throw new ValidationException("Last name is required.");
        }

        if (data.Document.Length != 11)
        {
            throw new ValidationException("CPF must contain 11 digits.");
        }

        if (!cpfService.Validate(data.Document))
        {
            throw new ValidationException("CPF is invalid.");
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

    private static ImportJobItemResponse ToJobItem(ImportJobRow row) => new()
    {
        Id = row.Id,
        JobPublicId = row.JobPublicId,
        Feature = row.Feature,
        FileName = row.FileName,
        CompanyId = row.CompanyId,
        Status = row.Status,
        TotalRows = row.TotalRows,
        ProcessedRows = row.ProcessedRows,
        SuccessRows = row.SuccessRows,
        ErrorRows = row.ErrorRows,
        ProgressPercent = CalculateProgressPercent(row.ProcessedRows, row.TotalRows),
        DurationMs = row.DurationMs,
        CreatedAtUtc = row.CreatedAtUtc,
        StartedAtUtc = row.StartedAtUtc,
        FinishedAtUtc = row.FinishedAtUtc,
        CreatedByUserId = row.CreatedByUserId
    };

    private static decimal CalculateProgressPercent(int processedRows, int totalRows)
    {
        if (totalRows <= 0)
        {
            return 0m;
        }

        var percent = (decimal)processedRows / totalRows * 100m;
        return Math.Round(percent, 2, MidpointRounding.AwayFromZero);
    }

    private static string? ResolveErrorField(ImportJobErrorRow error)
    {
        if (string.IsNullOrWhiteSpace(error.Message))
        {
            return null;
        }

        var message = error.Message.ToLowerInvariant();
        if (message.Contains("cpf"))
        {
            return "cpf";
        }

        if (message.Contains("e-mail") || message.Contains("email"))
        {
            return "email";
        }

        if (message.Contains("first name") || message.Contains("nome"))
        {
            return "nome";
        }

        if (message.Contains("last name") || message.Contains("sobrenome"))
        {
            return "sobrenome";
        }

        if (message.Contains("birth date") || message.Contains("nascimento"))
        {
            return "dt_nascimento";
        }

        return null;
    }

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

    private static (string Title, string Message)? BuildNotification(string status, string fileName)
    {
        return status switch
        {
            ImportJobStatus.Completed => (
                "Importação concluída",
                $"A importação do arquivo {fileName} foi concluída com sucesso."),
            ImportJobStatus.CompletedWithErrors => (
                "Importação concluída com erros",
                $"A importação do arquivo {fileName} foi concluída com erros. Consulte os detalhes."),
            ImportJobStatus.Failed => (
                "Importação falhou",
                $"A importação do arquivo {fileName} falhou. Consulte os detalhes."),
            ImportJobStatus.Cancelled => (
                "Importação cancelada",
                $"A importação do arquivo {fileName} foi cancelada."),
            _ => null
        };
    }

    private static string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Se contém ponto-e-vírgula, aspas duplas ou quebra de linha, envolver em aspas
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            // Escapar aspas duplas duplicando-as
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}
