using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Dapper;
using FluentValidation;
using Partner.Api.Features.Import;
using Partner.Api.Infrastructure.Database;
using Partner.Api.Infrastructure.Integrations.Vtex;
using Partner.Api.Infrastructure.Security;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Partner.Api.Infrastructure.Import;

public sealed class ImportWorker : BackgroundService
{
    private readonly ImportRabbitMqConnectionFactory _connectionFactory;
    private readonly ImportRabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ImportWorker> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ICpfProtectionService _cpfService;

    private const int ProgressBatchSize = 20;

    private IConnection? _connection;
    private IModel? _channel;

    public ImportWorker(
        ImportRabbitMqConnectionFactory connectionFactory,
        IOptions<ImportRabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        IHostEnvironment hostEnvironment,
        ILogger<ImportWorker> logger,
        ICpfProtectionService cpfService)
    {
        _connectionFactory = connectionFactory;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        _cpfService = cpfService;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _connection = _connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);

        var queueArguments = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = _options.Exchange,
            ["x-dead-letter-routing-key"] = ImportJobPublisher.JobDlqRoutingKey
        };

        _channel.QueueDeclare(_options.ImportJobsQueue, durable: true, exclusive: false, autoDelete: false, arguments: queueArguments);
        _channel.QueueDeclare(_options.ImportDlqQueue, durable: true, exclusive: false, autoDelete: false);

        _channel.QueueBind(_options.ImportJobsQueue, _options.Exchange, ImportJobPublisher.JobQueuedRoutingKey);
        _channel.QueueBind(_options.ImportDlqQueue, _options.Exchange, ImportJobPublisher.JobDlqRoutingKey);

        _channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceivedAsync;

        _channel.BasicConsume(queue: _options.ImportJobsQueue, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel is null)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var messageJson = Encoding.UTF8.GetString(args.Body.ToArray());
            var message = JsonSerializer.Deserialize<ImportJobMessage>(messageJson);

            if (message is null)
            {
                _channel.BasicAck(args.DeliveryTag, false);
                return;
            }

            var correlationId = string.IsNullOrWhiteSpace(message.CorrelationId)
                ? args.BasicProperties?.CorrelationId ?? string.Empty
                : message.CorrelationId;

            using var scope = _scopeFactory.CreateScope();
            var jobRepository = scope.ServiceProvider.GetRequiredService<ImportJobRepository>();
            var sqlConnectionFactory = scope.ServiceProvider.GetRequiredService<ISqlConnectionFactory>();

            var job = await jobRepository.GetByPublicIdAsync(message.Job.PublicId, CancellationToken.None);

            if (job is null)
            {
                _logger.LogWarning(
                    "Import job not found. JobPublicId={JobPublicId} CorrelationId={CorrelationId}",
                    message.Job.PublicId,
                    correlationId);
                _channel.BasicAck(args.DeliveryTag, false);
                return;
            }

            if (ImportJobStatus.IsTerminal(job.Status))
            {
                _logger.LogInformation(
                    "Import job already terminal. JobId={JobId} JobPublicId={JobPublicId} Status={Status} CorrelationId={CorrelationId}",
                    job.Id,
                    job.PublicId,
                    job.Status,
                    correlationId);
                _channel.BasicAck(args.DeliveryTag, false);
                return;
            }

            var lockedBy = Environment.MachineName;
            var marked = await jobRepository.MarkRunningAsync(job.Id, lockedBy, DateTime.UtcNow, CancellationToken.None);

            if (marked == 0)
            {
                _logger.LogInformation(
                    "Import job not in expected state. JobId={JobId} JobPublicId={JobPublicId} CorrelationId={CorrelationId}",
                    job.Id,
                    job.PublicId,
                    correlationId);
                _channel.BasicAck(args.DeliveryTag, false);
                return;
            }

            using var logScope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["JobId"] = job.Id,
                ["JobPublicId"] = job.PublicId
            });

            _logger.LogInformation(
                "ImportJobStarted JobId={JobId} JobPublicId={JobPublicId} CompanyId={CompanyId} Feature={Feature} CorrelationId={CorrelationId}",
                job.Id,
                job.PublicId,
                job.CompanyId,
                job.Feature,
                correlationId);

            using var connection = sqlConnectionFactory.CreateConnection();
            await connection.ExecuteAsync(new CommandDefinition(ImportQueries.EnsureImportTables));

            var companyMap = await connection.QueryFirstOrDefaultAsync<ImportCompanyMapRow>(new CommandDefinition(
                ImportQueries.GetCompanyMapById,
                new { CompanyId = job.CompanyId }));

            if (companyMap is null)
            {
                throw new ValidationException("Company is invalid or inactive.");
            }

            var counters = new ImportJobCounters();

            var fullPath = Path.Combine(_hostEnvironment.ContentRootPath, job.FilePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Import file not found at {fullPath}.");
            }

            if (job.Feature.Equals("clients-csv-selected", StringComparison.OrdinalIgnoreCase))
            {
                var selectedLines = JsonSerializer.Deserialize<IReadOnlyCollection<ImportSelectedLineRequest>>(
                    await File.ReadAllTextAsync(fullPath)) ?? [];

                foreach (var line in selectedLines.OrderBy(l => l.LineNumber))
                {
                    if (await ShouldCancelAsync(connection, job.Id, correlationId))
                    {
                        await HandleCancellationAsync(connection, job, correlationId);
                        _channel.BasicAck(args.DeliveryTag, false);
                        return;
                    }

                    counters.TotalRows++;

                    var data = new CsvImportLineData
                    {
                        LineNumber = line.LineNumber,
                        RawLine = BuildRawLine(line),
                        Action = NormalizeAction(line.Action),
                        FirstName = NormalizeText(line.FirstName, 120),
                        LastName = NormalizeText(line.LastName, 120),
                        Document = NormalizeDigits(line.Document, 11),
                        Email = NormalizeEmail(line.Email),
                        Gender = NormalizeGender(line.Gender),
                        BirthDate = NormalizeDate(line.BirthDate),
                        Department = NormalizeText(line.Department, 120),
                        Role = NormalizeText(line.Role, 120)
                    };

                    await ProcessLineWithIdempotencyAsync(connection, job, companyMap, data, correlationId, counters, CancellationToken.None);

                    if (counters.ProcessedRows % ProgressBatchSize == 0)
                    {
                        await UpdateProgressAsync(connection, job.Id, counters.ProcessedRows, counters.SuccessRows, counters.ErrorRows);
                    }
                }
            }
            else if (job.Feature.Equals("clients-csv", StringComparison.OrdinalIgnoreCase))
            {
                await using var stream = File.OpenRead(fullPath);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

                var headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    throw new ValidationException("CSV header is empty.");
                }

                ValidateHeader(headerLine);
                var lineNumber = 1;

                while (!reader.EndOfStream)
                {
                    if (await ShouldCancelAsync(connection, job.Id, correlationId))
                    {
                        await HandleCancellationAsync(connection, job, correlationId);
                        _channel.BasicAck(args.DeliveryTag, false);
                        return;
                    }

                    var line = await reader.ReadLineAsync();
                    lineNumber++;

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    counters.TotalRows++;

                    CsvImportLineData data;
                    try
                    {
                        data = ParseLine(lineNumber, line);
                    }
                    catch (Exception exception)
                    {
                        var parsed = TryParseLoosely(lineNumber, line);
                        await HandleLineFailureAsync(connection, job, parsed, lineNumber, line, exception, correlationId, counters);

                        if (counters.ProcessedRows % ProgressBatchSize == 0)
                        {
                            await UpdateProgressAsync(connection, job.Id, counters.ProcessedRows, counters.SuccessRows, counters.ErrorRows);
                        }

                        continue;
                    }

                    await ProcessLineWithIdempotencyAsync(connection, job, companyMap, data, correlationId, counters, CancellationToken.None);

                    if (counters.ProcessedRows % ProgressBatchSize == 0)
                    {
                        await UpdateProgressAsync(connection, job.Id, counters.ProcessedRows, counters.SuccessRows, counters.ErrorRows);
                    }
                }
            }
            else
            {
                throw new ValidationException("Feature not supported for async import.");
            }

            await UpdateProgressAsync(connection, job.Id, counters.ProcessedRows, counters.SuccessRows, counters.ErrorRows);

            stopwatch.Stop();

            var finalStatus = counters.ErrorRows == 0
                ? ImportJobStatus.Completed
                : counters.SuccessRows == 0
                    ? ImportJobStatus.Failed
                    : ImportJobStatus.CompletedWithErrors;

            await connection.ExecuteAsync(new CommandDefinition(
                ImportQueries.UpdateImportJob,
                new
                {
                    Id = job.Id,
                    Status = finalStatus,
                    TotalRows = counters.TotalRows,
                    ProcessedRows = counters.ProcessedRows,
                    SuccessRows = counters.SuccessRows,
                    ErrorRows = counters.ErrorRows,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    FinishedAtUtc = DateTime.UtcNow,
                    Attempts = job.Attempts,
                    LastError = (string?)null,
                    LockedBy = (string?)null,
                    LockedAtUtc = (DateTime?)null,
                    LastHeartbeatAtUtc = DateTime.UtcNow
                }));

            var notification = BuildNotification(finalStatus, job.FileName);
            if (notification is not null)
            {
                var notificationId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    ImportQueries.InsertImportNotification,
                    new
                    {
                        ImportJobId = job.Id,
                        UserId = job.CreatedByUserId,
                        Title = notification.Value.Title,
                        Message = notification.Value.Message,
                        Status = ImportNotificationStatus.Unread,
                        CreatedAtUtc = DateTime.UtcNow,
                        ReadAtUtc = (DateTime?)null
                    }));

                _logger.LogInformation(
                    "NotificationCreated NotificationId={NotificationId} JobId={JobId} JobPublicId={JobPublicId} UserId={UserId} Status={Status} CorrelationId={CorrelationId}",
                    notificationId,
                    job.Id,
                    job.PublicId,
                    job.CreatedByUserId,
                    finalStatus,
                    correlationId);
            }

            _logger.LogInformation(
                "ImportCompleted JobId={JobId} JobPublicId={JobPublicId} CompanyId={CompanyId} Feature={Feature} Status={Status} TotalRows={TotalRows} SuccessRows={SuccessRows} ErrorRows={ErrorRows} CorrelationId={CorrelationId} ElapsedMs={ElapsedMs}",
                job.Id,
                job.PublicId,
                job.CompanyId,
                job.Feature,
                finalStatus,
                counters.TotalRows,
                counters.SuccessRows,
                counters.ErrorRows,
                correlationId,
                (int)stopwatch.ElapsedMilliseconds);

            // Publicar para VTEX se importação foi bem-sucedida (Completed)
            if (finalStatus == ImportJobStatus.Completed)
            {
                try
                {
                    using var vtexScope = _scopeFactory.CreateScope();
                    var vtexOptions = vtexScope.ServiceProvider.GetRequiredService<IOptions<VtexOptions>>().Value;
                    var vtexRabbitOptions = vtexScope.ServiceProvider.GetRequiredService<IOptions<VtexRabbitMqOptions>>().Value;
                    var vtexConnectionFactory = vtexScope.ServiceProvider.GetRequiredService<VtexRabbitMqConnectionFactory>();
                    var vtexLogger = vtexScope.ServiceProvider.GetRequiredService<ILogger<ImportWorker>>();

                    var vtexMessage = new VtexSyncMessage
                    {
                        MessageId = Guid.NewGuid().ToString("N"),
                        CorrelationId = correlationId,
                        JobId = job.Id,
                        JobPublicId = job.PublicId,
                        Feature = job.Feature,
                        CompanyId = job.CompanyId,
                        UserId = job.CreatedByUserId,
                        TotalRows = counters.TotalRows,
                        SuccessRows = counters.SuccessRows,
                        ErrorRows = counters.ErrorRows,
                        DurationMs = (int)stopwatch.ElapsedMilliseconds,
                        CompletedAtUtc = DateTime.UtcNow,
                        JobDataUri = $"/api/import/jobs/{job.PublicId}"
                    };

                    VtexSyncPublisher.PublishIfEnabled(
                        vtexOptions,
                        vtexConnectionFactory,
                        vtexRabbitOptions,
                        vtexMessage,
                        vtexLogger);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to publish VTEX sync message for completed import. JobId={JobId} JobPublicId={JobPublicId}",
                        job.Id,
                        job.PublicId);
                    // Não abortar processamento do Import mesmo se VTEX falhar
                }
            }

            _channel.BasicAck(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Import job failed during processing.");
            _channel.BasicNack(args.DeliveryTag, false, requeue: false);
        }
    }

    private async Task ProcessLineWithIdempotencyAsync(
        System.Data.IDbConnection connection,
        ImportJobDetail job,
        ImportCompanyMapRow company,
        CsvImportLineData data,
        string correlationId,
        ImportJobCounters counters,
        CancellationToken cancellationToken)
    {
        var lineHash = ComputeSha256(data.RawLine);
        var inserted = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            ImportQueries.TryInsertImportJobItem,
            new
            {
                ImportJobId = job.Id,
                Seq = data.LineNumber,
                LineHash = lineHash,
                Status = "Processing",
                ProcessedAtUtc = DateTime.UtcNow,
                ErrorId = (int?)null,
                TargetKey = (string?)null
            },
            cancellationToken: cancellationToken));

        if (inserted == 0)
        {
            _logger.LogInformation(
                "ImportRowSkipped JobId={JobId} JobPublicId={JobPublicId} Feature={Feature} LineNumber={LineNumber} CorrelationId={CorrelationId}",
                job.Id,
                job.PublicId,
                job.Feature,
                data.LineNumber,
                correlationId);
            return;
        }

        try
        {
            await ProcessLineAsync(connection, company, data, cancellationToken);
            counters.SuccessRows++;
            counters.ProcessedRows++;

            await connection.ExecuteAsync(new CommandDefinition(
                ImportQueries.UpdateImportJobItem,
                new
                {
                    ImportJobId = job.Id,
                    Seq = data.LineNumber,
                    Status = "Processed",
                    ProcessedAtUtc = DateTime.UtcNow,
                    ErrorId = (int?)null,
                    TargetKey = (string?)null
                },
                cancellationToken: cancellationToken));

            _logger.LogInformation(
                "ImportRowProcessed JobId={JobId} JobPublicId={JobPublicId} Feature={Feature} LineNumber={LineNumber} CorrelationId={CorrelationId}",
                job.Id,
                job.PublicId,
                job.Feature,
                data.LineNumber,
                correlationId);
        }
        catch (Exception exception)
        {
            counters.ProcessedRows++;
            counters.ErrorRows++;

            var errorId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                ImportQueries.InsertImportJobErrorWithId,
                new
                {
                    ImportJobId = job.Id,
                    Seq = data.LineNumber,
                    LineNumber = data.LineNumber,
                    ErrorCode = (string?)null,
                    Message = exception.Message,
                    RawLine = data.RawLine,
                    Action = data.Action,
                    Document = data.Document,
                    Email = data.Email,
                    CreatedAtUtc = DateTime.UtcNow
                },
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                ImportQueries.UpdateImportJobItem,
                new
                {
                    ImportJobId = job.Id,
                    Seq = data.LineNumber,
                    Status = "Failed",
                    ProcessedAtUtc = DateTime.UtcNow,
                    ErrorId = errorId,
                    TargetKey = (string?)null
                },
                cancellationToken: cancellationToken));

            _logger.LogWarning(exception,
                "ImportRowFailed JobId={JobId} JobPublicId={JobPublicId} Feature={Feature} LineNumber={LineNumber} Document={Document} CorrelationId={CorrelationId}",
                job.Id,
                job.PublicId,
                job.Feature,
                data.LineNumber,
                _cpfService.Mask(data.Document),
                correlationId);
        }
    }

    private async Task HandleLineFailureAsync(
        System.Data.IDbConnection connection,
        ImportJobDetail job,
        CsvImportLineData? parsed,
        int lineNumber,
        string rawLine,
        Exception exception,
        string correlationId,
        ImportJobCounters counters)
    {
        counters.ProcessedRows++;
        counters.ErrorRows++;

        var errorId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            ImportQueries.InsertImportJobErrorWithId,
            new
            {
                ImportJobId = job.Id,
                Seq = lineNumber,
                LineNumber = lineNumber,
                ErrorCode = (string?)null,
                Message = exception.Message,
                RawLine = rawLine,
                Action = parsed?.Action,
                Document = parsed?.Document,
                Email = parsed?.Email,
                CreatedAtUtc = DateTime.UtcNow
            }));

        var lineHash = ComputeSha256(rawLine);
        var inserted = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            ImportQueries.TryInsertImportJobItem,
            new
            {
                ImportJobId = job.Id,
                Seq = lineNumber,
                LineHash = lineHash,
                Status = "Failed",
                ProcessedAtUtc = DateTime.UtcNow,
                ErrorId = errorId,
                TargetKey = (string?)null
            }));

        if (inserted == 0)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                ImportQueries.UpdateImportJobItem,
                new
                {
                    ImportJobId = job.Id,
                    Seq = lineNumber,
                    Status = "Failed",
                    ProcessedAtUtc = DateTime.UtcNow,
                    ErrorId = errorId,
                    TargetKey = (string?)null
                }));
        }

         _logger.LogWarning(exception,
            "ImportRowFailed JobId={JobId} JobPublicId={JobPublicId} Feature={Feature} LineNumber={LineNumber} Document={Document} CorrelationId={CorrelationId}",
            job.Id,
            job.PublicId,
            job.Feature,
            lineNumber,
            _cpfService.Mask(parsed?.Document ?? string.Empty),
            correlationId);
    }

    private static async Task UpdateProgressAsync(
        System.Data.IDbConnection connection,
        int jobId,
        int processedRows,
        int successRows,
        int errorRows)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            ImportQueries.UpdateImportJobProgress,
            new
            {
                Id = jobId,
                ProcessedRows = processedRows,
                SuccessRows = successRows,
                ErrorRows = errorRows,
                LastHeartbeatAtUtc = DateTime.UtcNow
            }));
    }

    private static async Task<bool> ShouldCancelAsync(System.Data.IDbConnection connection, int jobId, string correlationId)
    {
        var state = await connection.QueryFirstOrDefaultAsync<(bool CancelRequested, string Status)>(new CommandDefinition(
            ImportQueries.GetImportJobCancellationStatus,
            new { Id = jobId }));

        return state.CancelRequested
            || string.Equals(state.Status, ImportJobStatus.CancellationRequested, StringComparison.OrdinalIgnoreCase);
    }

    private async Task HandleCancellationAsync(System.Data.IDbConnection connection, ImportJobDetail job, string correlationId)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            ImportQueries.UpdateImportJobCancellation,
            new
            {
                Id = job.Id,
                Status = ImportJobStatus.Cancelled,
                CancelRequested = true,
                CancelRequestedAtUtc = DateTime.UtcNow,
                CancelledAtUtc = DateTime.UtcNow
            }));

        var notification = BuildNotification(ImportJobStatus.Cancelled, job.FileName);
        if (notification is not null)
        {
            var notificationId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                ImportQueries.InsertImportNotification,
                new
                {
                    ImportJobId = job.Id,
                    UserId = job.CreatedByUserId,
                    Title = notification.Value.Title,
                    Message = notification.Value.Message,
                    Status = ImportNotificationStatus.Unread,
                    CreatedAtUtc = DateTime.UtcNow,
                    ReadAtUtc = (DateTime?)null
                }));

            _logger.LogInformation(
                "NotificationCreated NotificationId={NotificationId} JobId={JobId} JobPublicId={JobPublicId} UserId={UserId} Status={Status} CorrelationId={CorrelationId}",
                notificationId,
                job.Id,
                job.PublicId,
                job.CreatedByUserId,
                ImportJobStatus.Cancelled,
                correlationId);
        }

        _logger.LogInformation(
            "ImportCancelled JobId={JobId} JobPublicId={JobPublicId} Status={Status} CorrelationId={CorrelationId}",
            job.Id,
            job.PublicId,
            ImportJobStatus.Cancelled,
            correlationId);
    }

    private static string BuildRawLine(ImportSelectedLineRequest line)
    {
        return string.Join(';',
            line.FirstName,
            line.LastName,
            line.Document,
            line.Email,
            line.Gender,
            line.BirthDate,
            line.Department,
            line.Role,
            line.Action);
    }

    private static CsvImportLineData ParseLine(int lineNumber, string line)
    {
        var parts = line.Split(';');
        if (parts.Length != 9)
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
        CancellationToken cancellationToken)
    {
        // ValidateLineRules(data); // Comentado - será substituído por this.ValidateLineRules na implementação de instância

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

    private void ValidateLineRules(CsvImportLineData data)
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

        if (!_cpfService.Validate(data.Document))
        {
            throw new ValidationException("CPF is invalid.");
        }

        if (!string.IsNullOrWhiteSpace(data.Email) && !System.Text.RegularExpressions.Regex.IsMatch(data.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            throw new ValidationException("E-mail is invalid.");
        }

        if (data.Action is not "inserir" and not "atualizar" and not "excluir")
        {
            throw new ValidationException("Action must be inserir, atualizar or excluir.");
        }
    }

    private static void ValidateHeader(string headerLine)
    {
        var expectedColumns = new[]
        {
            "nome",
            "sobrenome",
            "cpf",
            "email",
            "sexo",
            "dt_nascimento",
            "departamento",
            "cargo",
            "acao"
        };

        var columns = headerLine.Split(';').Select(v => v.Trim().ToLowerInvariant()).ToArray();
        if (columns.Length != expectedColumns.Length)
        {
            throw new ValidationException("CSV header has invalid number of columns.");
        }

        for (var i = 0; i < expectedColumns.Length; i++)
        {
            if (!string.Equals(columns[i], expectedColumns[i], StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException($"CSV header mismatch at column {i + 1}. Expected '{expectedColumns[i]}'.");
            }
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

        return NormalizeText(candidate, 1);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(bytes)).ToLowerInvariant();
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

    private static class ImportNotificationStatus
    {
        public const string Unread = "Unread";
        public const string Read = "Read";
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();
        return base.StopAsync(cancellationToken);
    }

    private sealed class ImportJobCounters
    {
        public int TotalRows { get; set; }
        public int ProcessedRows { get; set; }
        public int SuccessRows { get; set; }
        public int ErrorRows { get; set; }
    }
}