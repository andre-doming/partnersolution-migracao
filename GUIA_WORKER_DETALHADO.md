# 📡 GUIA COMPLETO DO WORKER - EXPLICAÇÃO DETALHADA

**Objetivo**: Entender 100% como o worker funciona  
**Tempo de leitura**: 15 minutos  
**Nível**: Completo

---

## ❓ O QUE É UM WORKER?

Um **Worker** é um **serviço de background** que executa tarefas pesadas sem bloquear o usuário.

### Analogia do mundo real:

```
RESTAURANTE SEM WORKER (SEM FILA):
┌─────────────────┐
│ Cliente chega   │
│       ↓         │
│ Chef começa     │
│ Cozinha prato   │
│ (CLIENTE ESPERA)│ ← Bloqueado!
│       ↓         │
│ Serve ao cliente│
└─────────────────┘
Problema: Se houver 10 clientes, cada um espera 20 min!


RESTAURANTE COM WORKER (COM FILA):
┌──────────────────────────────┐
│ Cliente chega e pede        │
│       ↓                      │
│ Recepcionista:              │
│ "Voltamos em 30 min" (✓)    │ ← Cliente recebe resposta LOGO
│       ↓                      │
│ [Vai para FILA]             │
│       ↓                      │
│ CHEF (Worker) começa:       │
│ - Pega pedido da fila       │
│ - Cozinha (sem pressa)      │
│ - Coloca aviso na mesa      │
│       ↓                      │
│ Cliente vem pegar resultado │
└──────────────────────────────┘
Benefício: 10 clientes = 30 min cada, não há fila de espera!
```

---

## 🏗️ ARQUITETURA DO WORKER NO NOSSO SISTEMA

### Fluxo Completo:

```
1. USUÁRIO FARÁ UPLOAD CSV
   │
   ├─→ POST /api/import/clients-csv/process-selected
   │   ├─ Valida arquivo
   │   ├─ Cria ImportJob no banco
   │   ├─ Publica mensagem em RabbitMQ
   │   └─ Retorna 200 para o usuário (RESPOSTA IMEDIATA!) ✓
   │
   └─→ WORKER (Background Service):
       ├─ Consome mensagem de RabbitMQ
       ├─ Lê arquivo CSV do disco
       ├─ Valida cada linha
       ├─ Para CADA LINHA:
       │  ├─ Verifica se já foi processada (hash)
       │  ├─ INSERT/UPDATE/DELETE no banco
       │  ├─ Salva erro se houver
       │  └─ Continua próxima linha
       ├─ Atualiza status do job
       ├─ Cria notificação para usuário
       └─ Pronto! (Usuário vê resultado no dashboard)
```

---

## 📦 COMPONENTES DO WORKER

### 1️⃣ RabbitMQ (Fila de Mensagens)

**O quê**: Sistema de fila distribuída  
**Para quê**: Comunicação entre API e Worker

```
┌─────────────────────────────────────┐
│         RabbitMQ (Fila)            │
│                                    │
│  Exchange: partner.import          │
│  ├─ Queue: import.jobs             │
│  │  ├─ Message 1: Job #1001       │
│  │  ├─ Message 2: Job #1002       │
│  │  └─ Message 3: Job #1003       │
│  │                                 │
│  └─ Queue: import.dlq (dead-letter)│
│     └─ Message: Job #999 (erro)   │
│                                    │
└─────────────────────────────────────┘

API publica → Worker consome ✓
```

**Arquivo**: `Migracao/Partner.Api/Infrastructure/Import/ImportRabbitMqConnectionFactory.cs`

**Configuração**:
```csharp
var queueArguments = new Dictionary<string, object>
{
    ["x-dead-letter-exchange"] = _options.Exchange,
    ["x-dead-letter-routing-key"] = ImportJobPublisher.JobDlqRoutingKey
};

_channel.QueueDeclare(
    _options.ImportJobsQueue,    // "import.jobs"
    durable: true,               // Persiste se RabbitMQ cair
    exclusive: false,            // Múltiplos workers podem consumir
    autoDelete: false,           // Não deleta automaticamente
    arguments: queueArguments    // DLQ para erros
);
```

---

### 2️⃣ ImportWorker (Serviço Background)

**Arquivo**: `Migracao/Partner.Api/Infrastructure/Import/ImportWorker.cs`

**Classe**: `public sealed class ImportWorker : BackgroundService`

**O que faz**:
```csharp
protected override Task ExecuteAsync(CancellationToken stoppingToken)
{
    // 1. Criar conexão RabbitMQ
    _connection = _connectionFactory.CreateConnection();
    _channel = _connection.CreateModel();
    
    // 2. Declarar exchange (envio de mensagens)
    _channel.ExchangeDeclare(
        _options.Exchange,        // "partner.import"
        ExchangeType.Topic,       // Topic: filtro by routing key
        durable: true,
        autoDelete: false
    );
    
    // 3. Declarar filas (recebimento)
    _channel.QueueDeclare(_options.ImportJobsQueue, ...);    // Jobs normais
    _channel.QueueDeclare(_options.ImportDlqQueue, ...);     // Erros
    
    // 4. Vincular fila ao exchange (routing)
    _channel.QueueBind(_options.ImportJobsQueue, _options.Exchange, 
                       ImportJobPublisher.JobQueuedRoutingKey);
    
    // 5. Configurar consumer (listener)
    var consumer = new AsyncEventingBasicConsumer(_channel);
    consumer.Received += OnMessageReceivedAsync;  // ← Chamado quando msg chega
    
    // 6. Consumir mensagens
    _channel.BasicConsume(queue: _options.ImportJobsQueue, autoAck: false, 
                         consumer: consumer);
    
    return Task.CompletedTask;  // Async service stays running
}
```

---

### 3️⃣ Método Principal: OnMessageReceivedAsync

**Linhas**: 72-330  
**O que faz**: Processa CADA mensagem que chega da fila

```csharp
private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
{
    // PASSO 1: Desserializar mensagem JSON
    var messageJson = Encoding.UTF8.GetString(args.Body.ToArray());
    var message = JsonSerializer.Deserialize<ImportJobMessage>(messageJson);
    
    // PASSO 2: Validar job
    var job = await jobRepository.GetByPublicIdAsync(message.Job.PublicId);
    if (job is null) return;  // Job não encontrado, skip
    
    // PASSO 3: Marcar como RUNNING (lock para outros workers)
    var marked = await jobRepository.MarkRunningAsync(job.Id, lockedBy, DateTime.UtcNow);
    if (marked == 0) return;  // Outro worker já pegou, skip
    
    // PASSO 4: Abrir conexão banco de dados
    using var connection = sqlConnectionFactory.CreateConnection();
    
    // PASSO 5: Garantir tabelas existem
    await connection.ExecuteAsync(new CommandDefinition(ImportQueries.EnsureImportTables));
    
    // PASSO 6: Obter dados da empresa
    var companyMap = await connection.QueryFirstOrDefaultAsync<ImportCompanyMapRow>(
        ImportQueries.GetCompanyMapById, new { CompanyId = job.CompanyId });
    
    // PASSO 7: Processar linhas do CSV
    if (job.Feature == "clients-csv-selected")
    {
        var selectedLines = JsonSerializer.Deserialize<IReadOnlyCollection<ImportSelectedLineRequest>>(
            await File.ReadAllTextAsync(fullPath)) ?? [];
        
        foreach (var line in selectedLines.OrderBy(l => l.LineNumber))
        {
            counters.TotalRows++;
            var data = new CsvImportLineData { ... };
            
            await ProcessLineWithIdempotencyAsync(
                connection, job, companyMap, data, correlationId, counters);
        }
    }
    
    // PASSO 8: Atualizar status final
    await connection.ExecuteAsync(ImportQueries.UpdateImportJob, new
    {
        Id = job.Id,
        Status = counters.ErrorRows == 0 ? "Completed" : "CompletedWithErrors",
        TotalRows = counters.TotalRows,
        ProcessedRows = counters.ProcessedRows,
        SuccessRows = counters.SuccessRows,
        ErrorRows = counters.ErrorRows,
        DurationMs = (int)stopwatch.ElapsedMilliseconds,
        FinishedAtUtc = DateTime.UtcNow
    });
    
    // PASSO 9: Criar notificação para usuário
    var notification = BuildNotification(finalStatus, job.FileName);
    if (notification is not null)
    {
        await connection.ExecuteScalarAsync<int>(
            ImportQueries.InsertImportNotification, new
            {
                ImportJobId = job.Id,
                UserId = job.CreatedByUserId,
                Title = notification.Value.Title,
                Message = notification.Value.Message,
                Status = "Unread",
                CreatedAtUtc = DateTime.UtcNow
            });
    }
    
    // PASSO 10: Confirmar recebimento (ACK)
    _channel.BasicAck(args.DeliveryTag, false);  // Mensagem processada com sucesso
}
```

---

### 4️⃣ Método Principal: ProcessLineWithIdempotencyAsync

**Linhas**: 380-470  
**O que faz**: Processa UMA LINHA garantindo que não duplica

```csharp
private async Task ProcessLineWithIdempotencyAsync(...)
{
    // PASSO 1: Computar hash da linha
    var lineHash = ComputeSha256(data.RawLine);
    
    // PASSO 2: Tentar inserir item da linha (idempotente!)
    var inserted = await connection.ExecuteScalarAsync<int>(
        ImportQueries.TryInsertImportJobItem, new
        {
            ImportJobId = job.Id,
            Seq = data.LineNumber,
            LineHash = lineHash,  // CHAVE PARA IDEMPOTÊNCIA!
            Status = "Processing"
        });
    
    // PASSO 3: Se já foi processada, skip
    if (inserted == 0)
    {
        _logger.LogInformation("Linha já processada, pulando...");
        return;  // ← IMPORTANTE: Evita duplicata!
    }
    
    // PASSO 4: Processar a linha (INSERT/UPDATE/DELETE)
    try
    {
        await ProcessLineAsync(connection, company, data);
        counters.SuccessRows++;
        
        // PASSO 5: Marcar como processada
        await connection.ExecuteAsync(
            ImportQueries.UpdateImportJobItem, new
            {
                ImportJobId = job.Id,
                Seq = data.LineNumber,
                Status = "Processed",
                ErrorId = (int?)null
            });
    }
    catch (Exception exception)
    {
        counters.ErrorRows++;
        
        // PASSO 6: Registrar erro
        var errorId = await connection.ExecuteScalarAsync<int>(
            ImportQueries.InsertImportJobErrorWithId, new
            {
                ImportJobId = job.Id,
                Message = exception.Message,
                RawLine = data.RawLine
            });
        
        // PASSO 7: Marcar como falha
        await connection.ExecuteAsync(
            ImportQueries.UpdateImportJobItem, new
            {
                ImportJobId = job.Id,
                Seq = data.LineNumber,
                Status = "Failed",
                ErrorId = errorId
            });
    }
}
```

---

### 5️⃣ Método Principal: ProcessLineAsync

**Linhas**: 720-789  
**O que faz**: Executa a ação (INSERT/UPDATE/DELETE) da linha

```csharp
private static async Task ProcessLineAsync(...)
{
    // PASSO 1: Buscar cliente existente
    var existing = await connection.QueryFirstOrDefaultAsync<ImportExistingClientRow>(
        ImportQueries.FindActiveClientByDocumentOrEmail, new
        {
            company.PartnerId,
            data.Document,
            data.Email
        });
    
    // PASSO 2: Se ação é INSERIR
    if (data.Action == "inserir")
    {
        if (existing is not null)
            throw new ValidationException("Cliente já existe!");
        
        await connection.ExecuteAsync(
            ImportQueries.InsertClient, new
            {
                data.FirstName,
                data.LastName,
                DocumentHash = data.Document,  // ✅ CORRIGIDO!
                data.Email,
                Registration = BuildRegistration(data),
                company.PartnerId,
                ClientGuid = Guid.NewGuid()
            });
        return;
    }
    
    // PASSO 3: Se ação é ATUALIZAR
    if (data.Action == "atualizar")
    {
        if (existing is null)
            throw new ValidationException("Cliente não encontrado!");
        
        await connection.ExecuteAsync(
            ImportQueries.UpdateClient, new
            {
                existing.Id,
                data.FirstName,
                data.LastName,
                DocumentHash = data.Document,  // ✅ CORRIGIDO!
                data.Email,
                Registration = BuildRegistration(data),
                company.PartnerId
            });
        return;
    }
    
    // PASSO 4: Se ação é DELETAR
    if (data.Action == "excluir")
    {
        if (existing is null)
            throw new ValidationException("Cliente não encontrado!");
        
        await connection.ExecuteAsync(
            ImportQueries.InactivateClient, new { existing.Id });
        return;
    }
    
    throw new ValidationException("Ação não suportada!");
}
```

---

## 🔄 CICLO DE VIDA COMPLETO DE UMA IMPORTAÇÃO

```
TEMPO 0: USUÁRIO FETA UPLOAD
├─→ API recebe arquivo
├─→ Valida header
├─→ Cria ImportJob (status: Queued)
├─→ Publica em RabbitMQ
└─→ Retorna job_id ao usuário (RÁPIDO! ⚡)

TEMPO 1-2s: WORKER CONSOME MENSAGEM
├─→ OnMessageReceivedAsync() executado
├─→ Marca job como RUNNING
├─→ Valida arquivo
└─→ Começa a processar linhas

TEMPO 2-10s: WORKER PROCESSA LINHAS
├─→ Para CADA linha:
│  ├─ Computa SHA256 hash
│  ├─ Verifica se já foi processada (idempotência)
│  ├─ Se não: processa INSERT/UPDATE/DELETE
│  ├─ Se sim: pula (já processada antes)
│  └─ Registra sucesso ou erro
├─→ A cada 20 linhas: atualiza progresso
└─→ Continua até fim do arquivo

TEMPO 10s: WORKER FINALIZA
├─→ Calcula estatísticas finais
├─→ Define status final (Completed / CompletedWithErrors / Failed)
├─→ Cria notificação para usuário
├─→ Faz ACK da mensagem (confirma recebimento)
└─→ Volta a escutar fila para próxima mensagem

TEMPO 11s: USUÁRIO VÊ RESULTADO
├─→ Abre dashboard
├─→ Vê notificação "Importação concluída"
├─→ Clica em job
├─→ Vê 100 linhas processadas, 0 erros ✓
└─→ Sucesso! 🎉
```

---

## 🎯 PRINCIPAIS BENEFÍCIOS DO WORKER

### 1️⃣ NÃO BLOQUEIA REQUISIÇÃO HTTP
```
❌ SEM WORKER:
POST /api/import → (aguarda 30s) → 200 OK

✅ COM WORKER:
POST /api/import → (aguarda 0.5s) → 200 OK ⚡
(Processamento acontece em background)
```

### 2️⃣ ESCALÁVEL
```
Com 1 Worker: pode processar 1 job por vez
Com 5 Workers: pode processar 5 jobs em paralelo!

RabbitMQ distribui mensagens automaticamente.
```

### 3️⃣ RESILIENTE
```
Se Worker cair enquanto processa:
├─ Mensagem volta para a fila (não foi ACK'd)
├─ Outro worker pickup
└─ Retentativa automática

Dead-letter queue para jobs que erraram >3x
```

### 4️⃣ RASTREÁVEL
```
Cada linha tem:
├─ Hash SHA256 (evita duplicatas)
├─ Status (Processing / Processed / Failed)
├─ Erro se houver
└─ Timestamp

Logs mostram:
├─ ImportJobStarted
├─ ImportRowProcessed (por linha)
├─ ImportCompleted
└─ Correlação entre todas
```

### 5️⃣ IDEMPOTENTE
```
Se a mesma mensagem for processada 2x:
├─ Primeira: insere linha no banco ✓
├─ Segunda: vê que já existe (hash), pula ✓
└─ Resultado: sem duplicatas!
```

---

## 📊 TABELAS ENVOLVIDAS

### ImportJobs
```sql
CREATE TABLE dbo.ImportJobs (
    id INT PRIMARY KEY,
    public_id UNIQUEIDENTIFIER,
    status VARCHAR(50),           -- Queued / Running / Completed / Failed
    total_rows INT,
    processed_rows INT,
    success_rows INT,
    error_rows INT,
    started_at_utc DATETIME2,
    finished_at_utc DATETIME2,
    ...
)
```

### ImportJobItems (Rastreamento de linhas)
```sql
CREATE TABLE dbo.ImportJobItems (
    import_job_id INT,            -- FK para ImportJobs
    seq INT,                       -- Número da linha
    line_hash VARCHAR(64),         -- SHA256 (para idempotência)
    status VARCHAR(50),            -- Processing / Processed / Failed
    error_id INT,                  -- FK para erro se houver
    ...
)

-- Índice único previne duplicatas:
CREATE UNIQUE INDEX UX_ImportJobItems 
ON importJobItems(import_job_id, seq, line_hash)
```

### ImportJobErrors (Log de erros)
```sql
CREATE TABLE dbo.ImportJobErrors (
    id INT PRIMARY KEY,
    import_job_id INT,             -- FK para ImportJobs
    line_number INT,
    action VARCHAR(20),
    document VARCHAR(20),
    email VARCHAR(100),
    message NVARCHAR(MAX),         -- Erro completo
    ...
)
```

### ImportNotifications (Avisos para usuário)
```sql
CREATE TABLE dbo.ImportNotifications (
    id INT PRIMARY KEY,
    import_job_id INT,             -- ✅ ADICIONADO!
    user_id INT,
    title VARCHAR(200),
    message NVARCHAR(MAX),
    status VARCHAR(50),            -- Unread / Read
    created_at_utc DATETIME2,
    ...
)

-- Foreign key garantida:
ALTER TABLE dbo.ImportNotifications
ADD CONSTRAINT FK_ImportNotifications_ImportJobs
FOREIGN KEY (import_job_id) REFERENCES dbo.ImportJobs(id)
```

---

## 🔌 COMO REGISTRAR O WORKER NA API

**Arquivo**: `Migracao/Partner.Api/Program.cs`

```csharp
// ✅ Registrar como Hosted Service (background service)
services.AddHostedService<ImportWorker>();

// ✅ Registrar Factory de conexão RabbitMQ
services.AddSingleton<ImportRabbitMqConnectionFactory>();

// ✅ Configurar opções RabbitMQ
services.Configure<ImportRabbitMqOptions>(
    configuration.GetSection("RabbitMq:Import"));
```

**Arquivo**: `appsettings.json`

```json
{
  "RabbitMq": {
    "Import": {
      "Host": "localhost",
      "Port": 5672,
      "Username": "guest",
      "Password": "guest",
      "Exchange": "partner.import",
      "ImportJobsQueue": "import.jobs",
      "ImportDlqQueue": "import.dlq"
    }
  }
}
```

---

## 🧪 COMO TESTAR O WORKER

### Teste 1: Verificar que está rodando
```bash
# Logs devem mostrar:
[*] Starting Partner.Api server...
[INF] Now listening on: https://localhost:7111
[INF] Application started. Press Ctrl+C to shut down.

# Se vir "ImportWorker" sendo registrado = ✓ Worker ativo!
```

### Teste 2: Fazer importação
```bash
curl -X POST http://localhost:5205/api/import/clients-csv/process-selected \
  -H "Content-Type: application/json" \
  -d '[{"lineNumber": 1, "firstName": "João", "lastName": "Silva", ...}]'

# Resposta:
# 200 OK com job_id
# (Processamento em background)
```

### Teste 3: Monitorar progresso
```bash
# GET para ver status do job
curl http://localhost:5205/api/import/jobs/[job_id]

# Resposta deve mostrar:
# status: "Completed"
# success_rows: 100
# error_rows: 0
```

### Teste 4: Verificar RabbitMQ

Abra no browser: `http://localhost:15672`

```
Username: guest
Password: guest

Vai ver:
├─ Exchanges: partner.import
│  └─ Bindings: import.jobs queue
├─ Queues: import.jobs (x mensagens)
└─ Connections: (seu worker conectado)
```

---

## 🐛 TROUBLESHOOTING DO WORKER

### "Worker não está processando mensagens"

**Checklist**:
```
☐ RabbitMQ está rodando? → docker ps | grep rabbitmq
☐ Connection string correta? → appsettings.json
☐ Queue existe? → RabbitMQ Management UI
☐ Worker registrado? → Verificar Program.cs
☐ Logs mostram OnMessageReceivedAsync? → Sim = ✓
```

### "Mensagem fica na fila e não é processada"

```
Causas possíveis:
1. Worker crashed → Verificar logs de erro
2. BasicAck não foi chamado → Mensagem fica "pending"
3. Prefetch muito baixo → Aumentar em Program.cs
```

### "Linha processada 2x (duplicata)"

```
Não deveria acontecer! Verificar:
☐ Line hash está sendo computado? → Sim
☐ TryInsertImportJobItem retorna 0 segunda vez? → Deveria
☐ Índice unique está criado? → Sim (SQL_FIX_SCHEMA.sql)
```

---

## 💡 RESUMO EM UMA PÁGINA

| Item | O Quê | Por Quê |
|------|-------|--------|
| **Worker** | Serviço background que processa imports | Não bloqueia usuário |
| **RabbitMQ** | Fila de mensagens | API e Worker se comunicam |
| **OnMessageReceivedAsync** | Método que processa cada mensagem | Coração do worker |
| **ProcessLineAsync** | Executa INSERT/UPDATE/DELETE | Lógica de negócio |
| **Idempotência** | Garante sem duplicatas | Hash previne reprocessamento |
| **Notificação** | Aviso para usuário quando termina | UX: usuário sabe resultado |
| **Dead-Letter Queue** | Fila de erros | Falhas >3x vão para DLQ |

---

## 🎓 CONCEITOS-CHAVE

### Async/Await
```csharp
// Enquanto ProcessLineAsync executa (IO wait), thread está livre
await ProcessLineAsync(connection, company, data);

// Não bloqueia = pode processar múltiplos jobs em paralelo
```

### Idempotência
```csharp
// Mesma entrada = mesmo resultado SEMPRE
// Processar 1x ou 100x = resultado idêntico

lineHash = ComputeSha256(data.RawLine);  // Sempre igual para mesma linha
// Se exists(hash): skip
// Se not exists(hash): process
```

### Transações
```csharp
// Inserir item + processar + atualizar status = tudo ou nada
using var transaction = connection.BeginTransaction();
try {
    // Operações todas aqui
    transaction.Commit();
} catch {
    transaction.Rollback();
}
```

---

## ✅ AGORA VOCÊ ENTENDE O WORKER!

Você sabe:

✅ **O que é**: Serviço background que processa importações  
✅ **Como funciona**: RabbitMQ → Worker consome → Processa CSV → Notifica  
✅ **Por que existe**: API retorna logo, processamento em background  
✅ **Como evita duplicatas**: Hash SHA256 + unique index  
✅ **Como escala**: Múltiplos workers lendo mesma fila  
✅ **Como rastreia**: Logs + tabelas ImportJobItems e ImportJobErrors  

**Código está pronto?** ✅ Sim  
**Banco está pronto?** ✅ Sim (SQL_FIX_SCHEMA.sql)  
**Worker está explicado?** ✅ Sim (este arquivo!)

Agora você pode implementar com confiança! 🚀


