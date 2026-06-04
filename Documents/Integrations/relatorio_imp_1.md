# IMP-1 — Fundação da Importação Assíncrona (RabbitMQ)

## Resumo
Implementada a espinha dorsal da importação assíncrona conforme `Documents/Integrations/import_async_final_design.md`, cobrindo upload + criação de job + publicação no RabbitMQ + worker consumidor + transições de status + observabilidade básica.

**Fora do escopo (mantido):** processamento real do arquivo, validação de linhas, gravação de dados de domínio, integração VTEX, telas Angular, download de erros, notificações frontend e SignalR.

---

## Configuração RabbitMQ
Seção `RabbitMq` adicionada em `appsettings.json` e `appsettings.Development.json`:

```json
"RabbitMq": {
  "Host": "localhost",
  "Port": 5672,
  "VirtualHost": "partner",
  "Username": "guest",
  "Password": "guest",
  "Exchange": "partner.events",
  "ImportJobsQueue": "partner.import.jobs",
  "ImportDlqQueue": "partner.import.dlq"
}
```

Também adicionado `Import:StorageRoot` (pasta base dos uploads).

---

## Script SQL gerado
Arquivo versionado: **`sql_import_async_v1.sql`**

Contém as tabelas:
- `ImportJobs`
- `ImportJobErrors`
- `ImportNotifications`
- `ImportJobItems`

> O script **não é executado automaticamente**.

---

## Endpoints criados/ajustados

### Novo endpoint (async)
`POST /api/import/{feature}/csv/async`

Nesta fase:
- recebe o arquivo
- salva em disco
- calcula SHA256
- aplica idempotência de job
- cria registro `ImportJobs`
- publica evento RabbitMQ
- retorna `JobPublicId` + `Status=Queued`

---

## Worker (BackgroundService)

Criado `ImportWorker`:
- consome `partner.import.jobs`
- transição `Queued → Running → Completed`
- logs estruturados: `ImportJobStarted`, `ImportJobCompleted`
- configura DLQ `partner.import.dlq`

---

## Logs estruturados (Seq)
Eventos registrados:
- `ImportJobQueued`
- `ImportJobStarted`
- `ImportJobCompleted`

Campos mínimos presentes:
- `CorrelationId`
- `JobPublicId`
- `CompanyId`
- `Feature`

---

## Testes executados
```
dotnet build Partner.Modern.sln
dotnet test Partner.Modern.sln
```

Resultado:
- Build OK (1 warning pré-existente)
- Testes OK: **33**

---

## Arquivos criados
- `sql_import_async_v1.sql`
- `Documents/Integrations/relatorio_imp_1.md`
- `Migracao/Partner.Api/Infrastructure/Import/ImportRabbitMqOptions.cs`
- `Migracao/Partner.Api/Infrastructure/Import/ImportStorageOptions.cs`
- `Migracao/Partner.Api/Infrastructure/Import/ImportRabbitMqConnectionFactory.cs`
- `Migracao/Partner.Api/Infrastructure/Import/ImportJobMessage.cs`
- `Migracao/Partner.Api/Infrastructure/Import/ImportJobStatus.cs`
- `Migracao/Partner.Api/Infrastructure/Import/ImportJobStateMachine.cs`
- `Migracao/Partner.Api/Infrastructure/Import/ImportJobRequestFactory.cs`
- `Migracao/Partner.Api/Infrastructure/Import/ImportJobMessageFactory.cs`
- `Migracao/Partner.Api/Infrastructure/Import/ImportJobPublisher.cs`
- `Migracao/Partner.Api/Infrastructure/Import/ImportFileStorage.cs`
- `Migracao/Partner.Api/Infrastructure/Import/ImportJobRepository.cs`
- `Migracao/Partner.Api/Infrastructure/Import/ImportWorker.cs`
- `Migracao/Partner.Api.Tests/Import/ImportAsyncFoundationTests.cs`

## Arquivos alterados
- `Migracao/Partner.Api/appsettings.json`
- `Migracao/Partner.Api/appsettings.Development.json`
- `Migracao/Partner.Api/Partner.Api.csproj`
- `Migracao/Partner.Api.Tests/Partner.Api.Tests.csproj`
- `Migracao/Partner.Api/Shared/Extensions/ServiceCollectionExtensions.cs`
- `Migracao/Partner.Api/Features/Import/ImportModels.cs`
- `Migracao/Partner.Api/Features/Import/ImportQueries.cs`
- `Migracao/Partner.Api/Features/Import/ImportEndpoints.cs`

---

## Limitações da fase (mantidas)
- Sem processamento real do arquivo.
- Sem validação de linhas e gravação de domínio.
- Sem integração VTEX.
- Sem tela Angular.
- Sem download de erros.
- Sem notificações frontend.
- Sem SignalR.

---

## Observações
- Idempotência implementada apenas no **nível de job** (hash do arquivo + company + feature).
- Idempotência por item fica para a IMP-2.
