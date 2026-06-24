# 🚀 GUIA COMPLETO: COMO FUNCIONA O WORKER

---

## 📌 O QUE É O WORKER?

O **Worker** é um serviço **independente** que roda em **background** e processa importações CSV **assincronamente**.

```
USUÁRIO                    API                         WORKER
  │                         │                            │
  ├─ Envia CSV ────────────>│                            │
  │                         │                            │
  │<────── Job ID (200) ─────┤                            │
  │                         │                            │
  │                         ├─ Publica mensagem ────────>│
  │                         │  (via RabbitMQ)            │
  │                         │                     Processa
  │                         │                            │
  │                         │<─── Notifica ───────────────┤
  │                         │                            │
  │<─ Solicita histórico ────┤                            │
  │                         │                            │
  │<──── Job Completed ─────┤                            │
```

---

## 🔄 FLUXO DETALHADO DO WORKER

### ETAPA 1: RECEBIMENTO DE MENSAGEM

```csharp
// arquivo: ImportWorker.cs
public async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
{
    // 1. Deserializa a mensagem
    var message = JsonSerializer.Deserialize<ImportJobMessage>(args.Body);
    // Contém: JobId, Feature, FilePath, CorrelationId
    
    // 2. Abre conexão com banco
    using var connection = connectionFactory.CreateConnection();
    
    // 3. Carrega o Job
    var job = await connection.QueryFirstOrDefaultAsync<ImportJobDetail>(
        ImportQueries.GetImportJobByPublicId,
        new { PublicId = message.JobPublicId }
    );
    
    if (job == null) return; // Job não existe
    
    // 4. Valida se está em Queued
    if (job.Status != "Queued") return; // Outro worker está processando
}
```

**Mensagem esperada (JSON)**:
```json
{
  "JobPublicId": "5269cbc8-808c-47c6-9813-ef7a248bcb66",
  "Feature": "clients-csv-selected",
  "FilePath": "D:\\uploads\\import_1009.csv",
  "CorrelationId": "2f62735220cc49f3acf0908212209579"
}
```

---

### ETAPA 2: MARCA COMO RUNNING

```csharp
// Previne que outro worker processe o mesmo job
await connection.ExecuteAsync(
    ImportQueries.MarkImportJobRunning,
    new {
        Id = job.Id,
        Status = "Running",
        LockedBy = Environment.MachineName,
        LockedAtUtc = DateTime.UtcNow,
        ExpectedStatus = "Queued"  // Só atualiza se ainda estiver em Queued
    }
);
```

**Resultado NO BANCO**:
```sql
UPDATE dbo.ImportJobs
SET status = 'Running',
    locked_by = 'COMPUTADOR-ANDRE',
    locked_at_utc = '2026-06-08 22:42:00'
WHERE id = 1009 AND status = 'Queued';
```

---

### ETAPA 3: PROCESSA LINHAS DO CSV

```csharp
// Abre arquivo CSV
using var reader = new StreamReader(job.FilePath);
using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);

var lineNumber = 0;
var counters = new ImportJobCounters 
{ 
    Total = 0,
    Processed = 0,
    Success = 0,
    Error = 0
};

// Lê cada linha
while (await csvReader.ReadRecordAsync())
{
    lineNumber++;
    
    // 1. Extrai dados da linha
    var data = new CsvImportLineData
    {
        LineNumber = lineNumber,
        Document = csvReader.GetField("cpf"),
        FirstName = csvReader.GetField("nome"),
        LastName = csvReader.GetField("sobrenome"),
        Email = csvReader.GetField("email"),
        // ... mais campos
    };
    
    // 2. PROCESSA A LINHA
    await ProcessLineWithIdempotencyAsync(
        connection,
        job,
        company,
        data,
        correlationId,
        counters,
        cancellationToken
    );
}
```

---

### ETAPA 4: PROCESSA UMA LINHA (CRÍTICO!)

Aqui acontece a **mágica**:

```csharp
private async Task ProcessLineAsync(
    IDbConnection connection,
    ImportCompanyMapRow company,
    CsvImportLineData data,
    CancellationToken cancellationToken)
{
    try
    {
        // 1. CALCULA HASH DO CPF (para de duplicatas)
        var documentHash = CpfProtectionService.Normalize(data.Document);
        // cpf "123.456.789-00" -> "12345678900"
        
        // 2. BUSCA SE CLIENTE EXISTE
        var existingClient = await connection.QueryFirstOrDefaultAsync<ClientRow>(
            ImportQueries.FindActiveClientByDocumentOrEmail,
            new {
                PartnerId = company.PartnerId,
                Document = documentHash,
                Email = data.Email
            }
        );
        
        // 3. INSERE OU ATUALIZA
        if (existingClient != null)
        {
            // ATUALIZA cliente existente
            await connection.ExecuteAsync(
                ImportQueries.UpdateClient,
                new {
                    Id = existingClient.Id,
                    FirstName = data.FirstName,
                    LastName = data.LastName,
                    DocumentHash = documentHash,
                    Email = data.Email,
                    Registration = data.Registration,
                    PartnerId = company.PartnerId
                }
            );
        }
        else
        {
            // INSERE novo cliente
            await connection.ExecuteAsync(
                ImportQueries.InsertClient,
                new {
                    FirstName = data.FirstName,
                    LastName = data.LastName,
                    DocumentHash = documentHash,  // ⚠️ AQUI É O PROBLEMA!
                    Email = data.Email,
                    Registration = data.Registration,
                    PartnerId = company.PartnerId,
                    ClientGuid = Guid.NewGuid()
                }
            );
        }
        
        // 4. MARCA COMO SUCESSO
        counters.Success++;
    }
    catch (Exception ex)
    {
        // 5. TRATA ERRO
        counters.Error++;
        
        var errorId = await connection.ExecuteScalarAsync<int>(
            ImportQueries.InsertImportJobErrorWithId,
            new {
                ImportJobId = job.Id,
                LineNumber = data.LineNumber,
                ErrorCode = "CLIENT_INSERT_FAILED",
                Message = ex.Message,
                Action = data.Action,
                Document = data.Document,
                Email = data.Email,
                CreatedAtUtc = DateTime.UtcNow
            }
        );
        
        // Log para auditoria
        logger.LogError(
            "ImportRowFailed JobId={JobId} LineNumber={LineNumber} " +
            "Document={Document} Error={Error}",
            job.Id, data.LineNumber, data.Document, ex.Message
        );
    }
}
```

---

### ETAPA 5: IDEMPOTÊNCIA (PREVINE DUPLICATAS)

```csharp
private async Task ProcessLineWithIdempotencyAsync(
    IDbConnection connection,
    ImportJobDetail job,
    ImportCompanyMapRow company,
    CsvImportLineData data,
    string correlationId,
    ImportJobCounters counters,
    CancellationToken cancellationToken)
{
    // PROBLEMA: Se worker falha e reinicia?
    // Solução: Usar TryInsertImportJobItem para marcar que processamos
    
    // 1. Calcula HASH da linha
    var lineHash = ComputeLineHash(data);
    // "john|doe|12345678900|john@email.com" -> "abc123..."
    
    // 2. Tenta INSERT em ImportJobItems
    var inserted = await connection.ExecuteScalarAsync<int>(
        ImportQueries.TryInsertImportJobItem,
        new {
            ImportJobId = job.Id,
            Seq = data.LineNumber,
            LineHash = lineHash,
            Status = "Processing",
            ProcessedAtUtc = DateTime.UtcNow,
            ErrorId = (int?)null,
            TargetKey = null
        }
    );
    
    if (inserted == 0)
    {
        // Já foi processada! Pula
        logger.LogDebug(
            "Line {LineNumber} already processed (idempotent skip)",
            data.LineNumber
        );
        return;
    }
    
    // 3. Processa a linha
    await ProcessLineAsync(connection, company, data, cancellationToken);
    
    // 4. Marca como concluída
    await connection.ExecuteAsync(
        ImportQueries.UpdateImportJobItem,
        new {
            ImportJobId = job.Id,
            Seq = data.LineNumber,
            Status = "Success",
            ProcessedAtUtc = DateTime.UtcNow
        }
    );
}
```

**Como funciona**:

```sql
-- TryInsertImportJobItem verifica se já existe
INSERT INTO dbo.ImportJobItems(
    import_job_id, seq, line_hash, status, processed_at_utc
)
SELECT @ImportJobId, @Seq, @LineHash, @Status, @ProcessedAtUtc
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.ImportJobItems
    WHERE import_job_id = @ImportJobId AND seq = @Seq
);
-- Se inseriu (@@ROWCOUNT = 1) → processa
-- Se não inseriu (@@ROWCOUNT = 0) → já foi processado antes
```

---

### ETAPA 6: ATUALIZA PROGRESSO

```csharp
// A cada 100 linhas
if (lineNumber % 100 == 0)
{
    await UpdateProgressAsync(connection, job.Id, counters);
}

private async Task UpdateProgressAsync(
    IDbConnection connection,
    int jobId,
    ImportJobCounters counters)
{
    await connection.ExecuteAsync(
        ImportQueries.UpdateImportJobProgress,
        new {
            Id = jobId,
            ProcessedRows = counters.Processed,
            SuccessRows = counters.Success,
            ErrorRows = counters.Error,
            LastHeartbeatAtUtc = DateTime.UtcNow
        }
    );
}
```

**UI Atualiza em Tempo Real**:
- `[22:42:09] Processadas: 100 de 500 linhas`
- `[22:42:15] Processadas: 200 de 500 linhas`
- `[22:42:21] Processadas: 300 de 500 linhas`

---

### ETAPA 7: FINALIZA JOB

```csharp
// Após processar todas as linhas
var duration = (DateTime.UtcNow - job.StartedAtUtc).TotalMilliseconds;

await connection.ExecuteAsync(
    ImportQueries.UpdateImportJobStatus,
    new {
        Id = job.Id,
        Status = counters.Error > 0 ? "Completed_With_Errors" : "Completed",
        FinishedAtUtc = DateTime.UtcNow,
        DurationMs = (int)duration,
        LastError = null,
        CancelledAtUtc = (DateTime?)null,
        CancelRequested = false,
        CancelRequestedAtUtc = (DateTime?)null
    }
);
```

---

### ETAPA 8: NOTIFICA USUÁRIO

```csharp
// Cria notificação que usuário vê em "Histórico"
var notificationId = await connection.ExecuteScalarAsync<int>(
    ImportQueries.InsertImportNotification,
    new {
        ImportJobId = job.Id,
        UserId = job.CreatedByUserId,  // ⚠️ AQUI! Associa ao criador
        Title = $"Importação {data.FileName} concluída",
        Message = $"Sucesso: {counters.Success}, Erros: {counters.Error}",
        Status = "Completed",
        CreatedAtUtc = DateTime.UtcNow,
        ReadAtUtc = (DateTime?)null
    }
);

// Log
logger.LogInformation(
    "NotificationCreated NotificationId={NotificationId} " +
    "JobId={JobId} UserId={UserId} Status=Completed",
    notificationId, job.Id, job.CreatedByUserId
);
```

---

## 🏗️ ARQUITETURA: WORKER EM PROJETO SEPARADO

### Estrutura Recomendada

```
partnersolution/
├── Migracao/
│   ├── Partner.Api/                    (API REST)
│   │   ├── Features/
│   │   ├── Infrastructure/
│   │   └── appsettings.json
│   │
│   ├── Partner.Worker/                 ✨ NOVO PROJETO
│   │   ├── ImportProcessor.cs
│   │   ├── WorkerService.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   └── Partner.Worker.Tests/           (Testes do Worker)
│       └── ImportProcessorTests.cs
│
└── PartnerSolution.sln
```

### Partner.Worker.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>WinExe</OutputType>  <!-- ou Console -->
    <DockerDefaultTargetOS>linux</DockerDefaultTargetOS>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="RabbitMQ.Client" Version="6.5.0" />
    <PackageReference Include="Dapper" Version="2.0.123" />
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../Partner.Api/Partner.Api.csproj" />
  </ItemGroup>
</Project>
```

### Program.cs (Worker)

```csharp
using Partner.Worker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddRabbitMqConnection(configuration);
        services.AddSqlConnectionFactory(configuration);
        services.AddHostedService<ImportWorkerService>();
        services.AddScoped<IImportProcessor, ImportProcessor>();
    })
    .Build();

await host.RunAsync();
```

### Comunicação API ↔ Worker (RabbitMQ)

```csharp
// NA API: Após receber CSV, publica mensagem
var message = new ImportJobMessage
{
    JobPublicId = job.PublicId,
    Feature = "clients-csv-selected",
    FilePath = "/uploads/import_1009.csv",
    CorrelationId = CorrelationIdMiddleware.GetCorrelationId(httpContext)
};

await rabbitMqPublisher.PublishAsync("import.jobs", message);

// NO WORKER: Recebe do RabbitMQ
consumer.Received += async (model, args) =>
{
    var message = JsonSerializer.Deserialize<ImportJobMessage>(args.Body);
    await importProcessor.ProcessAsync(message);
};
```

---

## ⚠️ PROBLEMAS COMUNS

### 1. "Must declare the scalar variable "@DocumentHash""

**Problema**: O parâmetro não está sendo passado na query

```csharp
// ERRADO
await connection.ExecuteAsync(
    ImportQueries.InsertClient,
    new {
        FirstName = data.FirstName,
        LastName = data.LastName,
        // DocumentHash faltando! ❌
        Email = data.Email
    }
);

// CORRETO
var documentHash = CpfProtectionService.Normalize(data.Document);
await connection.ExecuteAsync(
    ImportQueries.InsertClient,
    new {
        FirstName = data.FirstName,
        LastName = data.LastName,
        DocumentHash = documentHash,  // ✅
        Email = data.Email
    }
);
```

### 2. "Histórico vazio"

**Problema**: Notificação criada com `UserId` diferente do usuário logado

```csharp
// Verificar
SELECT * FROM ImportNotifications 
WHERE import_job_id = 1009;

-- Se user_id = 2 mas usuário logado é 99
-- Então ListImportNotifications filtrará por user_id = 99
-- E não achará nada!
```

**Solução**:
```csharp
// Sempre usar o contexto do usuário
var userId = ResolveActorUserId(httpContext.User);  // ✅ Correto
// Não fazer isso:
// var userId = 2;  // ❌ Hardcoded!
```

---

## ✅ CHECKLIST: IMPLEMENTAR WORKER

- [ ] Criar projeto `Partner.Worker`
- [ ] Implementar `ImportWorkerService : BackgroundService`
- [ ] Implementar `IImportProcessor`
- [ ] Configurar RabbitMQ connection
- [ ] Adicionar logging estruturado
- [ ] Criar testes unitários
- [ ] Documentar deployment (Docker, Windows Service)
- [ ] Monitorar fila de messages
- [ ] Implementar retry automático
- [ ] Health checks

---

## 📊 MONITORAMENTO DO WORKER

```csharp
// Adicionar health check
services.AddHealthChecks()
    .AddCheck("RabbitMQ", async () => {
        // Verifica conexão RabbitMQ
    })
    .AddCheck("Database", async () => {
        // Verifica conexão SQL
    });

// Endpoint /health
app.MapHealthChecks("/health");
```

**Métricas para monitorar**:
- ✅ Mensagens processadas por minuto
- ✅ Taxa de erro
- ✅ Tempo médio de processamento
- ✅ Tamanho da fila RabbitMQ
- ✅ Conexões com banco de dados


