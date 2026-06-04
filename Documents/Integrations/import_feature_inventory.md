# Inventário Consolidado da Feature Import

**Data de Geração:** 2026-06-04  
**Status:** Mapeamento do estado atual do código real  
**Escopo:** Feature de Importação CSV (síncrona e assíncrona)

---

## 1. Endpoints Existentes

### 1.1 Endpoints de Importação (sincronamente)

#### `POST /api/import/clients-csv`
- **Tipo:** Upload e processamento síncrono
- **Autenticação:** Requerida (AuthPolicies.Import)
- **Content-Type:** multipart/form-data
- **Parâmetros:**
  - `file`: IFormFile (CSV até 5MB)
  - `companyId`: int (obrigatório, > 0)
- **Resposta:** ImportClientsCsvResponse
  - Status (Completed, CompletedWithErrors, Failed)
  - Contadores (TotalRows, SuccessRows, ErrorRows)
  - Lista de erros por linha
  - Duração em ms
- **Validações:**
  - Arquivo obrigatório e não vazio
  - Tamanho máximo 5MB
  - CSV UTF-8 com encoding automático
  - Cabeçalho com 9 colunas esperadas

#### `POST /api/import/{feature}/csv/async`
- **Tipo:** Upload e processamento assíncrono via RabbitMQ
- **Autenticação:** Requerida
- **Content-Type:** multipart/form-data
- **Parâmetros:**
  - `feature`: string (rota dinâmica)
  - `file`: IFormFile
  - `companyId`: int
- **Resposta:** ImportAsyncUploadResponse
  - JobPublicId: Guid
  - Status: "Queued"
- **Fluxo:** Arquivo armazenado, job criado e publicado em RabbitMQ

#### `POST /api/import/clients-csv/preview`
- **Tipo:** Preview sem persistência
- **Autenticação:** Requerida
- **Content-Type:** multipart/form-data
- **Resposta:** ImportPreviewCsvResponse
  - FileName, CompanyId, TotalRows
  - ValidRows, InvalidRows
  - Lista de linhas com status de validação individual

#### `POST /api/import/clients-csv/process-selected`
- **Tipo:** Processamento assíncrono de linhas selecionadas
- **Autenticação:** Requerida
- **Content-Type:** application/json
- **Body:** ImportProcessSelectedRequest
  - CompanyId: int
  - FileName: string
  - SelectedLines: array de ImportSelectedLineRequest
- **Resposta:** ImportAsyncUploadResponse

### 1.2 Endpoints de Lookup e Consulta

#### `GET /api/import/lookups`
- **Descrição:** Empresas disponíveis para o usuário autenticado
- **Resposta:** ClientLookupResponse
  - Companies: array com Id e Name

#### `GET /api/import/jobs`
- **Descrição:** Histórico de importações com paginação
- **Query Params:**
  - page: int (default: 1)
  - pageSize: int (default: 20, máx: 100)
  - status: string (opcional)
  - startDateUtc: DateTime (opcional)
  - endDateUtc: DateTime (opcional)
  - userId: int (apenas admins)
- **Resposta:** ImportJobListResponse
  - Paginação com total
  - Array de ImportJobItemResponse

#### `GET /api/import/jobs/{id:int}`
- **Descrição:** Detalhe de job por ID interno
- **Resposta:** ImportJobDetailResponse (ou 404)

#### `GET /api/import/jobs/{jobPublicId:guid}`
- **Descrição:** Detalhe de job por GUID público
- **Resposta:** ImportJobDetailResponse (ou 404)

#### `GET /api/import/jobs/{jobPublicId:guid}/errors`
- **Descrição:** Erros de um job específico com paginação
- **Query Params:**
  - page: int (default: 1)
  - pageSize: int (default: 20)
- **Resposta:** ImportJobErrorsResponse

### 1.3 Endpoints de Notificação

#### `GET /api/import/notifications`
- **Descrição:** Lista notificações do usuário autenticado
- **Resposta:** Array de ImportNotificationResponse

#### `POST /api/import/notifications/{id:int}/read`
- **Descrição:** Marca notificação como lida
- **Resposta:** 204 NoContent (ou 404)

#### `POST /api/import/notifications/read-all`
- **Descrição:** Marca todas as notificações como lidas
- **Resposta:** 204 NoContent

### 1.4 Endpoints de Controle de Job

#### `POST /api/import/jobs/{jobPublicId:guid}/cancel`
- **Descrição:** Solicita cancelamento de job em processamento
- **Resposta:** ImportJobActionResponse
  - Status final
  - Mensagem de confirmação

#### `POST /api/import/jobs/{jobPublicId:guid}/retry`
- **Descrição:** Solicita reprocessamento de job
- **Resposta:** ImportJobActionResponse
  - NewJobPublicId: Guid (novo job criado)

---

## 2. RabbitMQ (Publisher e Consumer)

### 2.1 Configuração

**Arquivo:** `Infrastructure/Import/ImportRabbitMqOptions.cs`

```
Host: (configurável)
Port: 5672 (padrão)
VirtualHost: partner
Username: (configurável)
Password: (configurável)
Exchange: partner.events
ImportJobsQueue: partner.import.jobs
ImportDlqQueue: partner.import.dlq
```

### 2.2 Publisher

**Arquivo:** `Infrastructure/Import/ImportJobPublisher.cs`

- **Função:** Publica mensagens de job queued
- **Routing Key:** `import.job.queued`
- **DLQ Routing Key:** `import.job.dlq`
- **Message Type:** ImportJobMessage (JSON serializado)
- **Triggers:**
  - `POST /api/import/{feature}/csv/async`
  - `POST /api/import/clients-csv/process-selected`

### 2.3 Consumer

**Arquivo:** `Infrastructure/Import/ImportWorker.cs`

- **Tipo:** BackgroundService
- **Fila:** partner.import.jobs
- **DLQ:** partner.import.dlq (dead-letter)
- **QoS:** Prefetch 1 (processa um job por vez)
- **Recursos Consumidos:**
  - Suporta CSV upload com múltiplas linhas
  - Suporta JSON com linhas selecionadas (clients-csv-selected)
- **Processamento:**
  - Obtém job do banco
  - Marca como Running
  - Processa arquivo linha por linha
  - Atualiza progresso a cada 20 linhas
  - Registra heartbeat
  - Suporta cancelamento mid-processing
  - Finaliza com notificação

---

## 3. Tabelas Utilizadas

### 3.1 Schema Principal de Importação

#### `ImportJobs`

```sql
CREATE TABLE dbo.ImportJobs
(
    id INT IDENTITY(1,1) PRIMARY KEY,
    public_id UNIQUEIDENTIFIER NOT NULL [UQ],
    feature NVARCHAR(60) NOT NULL,
    file_name NVARCHAR(260) NOT NULL,
    file_path NVARCHAR(500) NOT NULL,
    file_hash_sha256 CHAR(64) NOT NULL,
    company_id INT NOT NULL,
    status NVARCHAR(30) NOT NULL,
    total_rows INT NULL,
    processed_rows INT NOT NULL,
    success_rows INT NOT NULL,
    error_rows INT NOT NULL,
    duration_ms INT NOT NULL,
    started_at_utc DATETIME2 NOT NULL,
    finished_at_utc DATETIME2 NULL,
    created_by_user_id INT NOT NULL,
    cancel_requested BIT NOT NULL,
    cancel_requested_at_utc DATETIME2 NULL,
    attempts INT NOT NULL,
    last_error NVARCHAR(2000) NULL,
    locked_by NVARCHAR(100) NULL,
    locked_at_utc DATETIME2 NULL,
    last_heartbeat_at_utc DATETIME2 NULL,
    correlation_id NVARCHAR(100) NULL,
    cancelled_at_utc DATETIME2 NULL,
    retry_of_import_job_id INT NULL
);

CREATE UNIQUE INDEX UQ_ImportJobs_PublicId ON dbo.ImportJobs(public_id);
CREATE INDEX IX_ImportJobs_Company_Status_CreatedAt ON dbo.ImportJobs(company_id, status, started_at_utc DESC);
CREATE INDEX IX_ImportJobs_FileHash_Window ON dbo.ImportJobs(company_id, feature, file_hash_sha256, started_at_utc DESC);
```

#### `ImportJobErrors`

```sql
CREATE TABLE dbo.ImportJobErrors
(
    id INT IDENTITY(1,1) PRIMARY KEY,
    import_job_id INT NOT NULL [FK],
    seq INT NOT NULL,
    line_number INT NOT NULL,
    error_code NVARCHAR(50) NULL,
    message NVARCHAR(1000) NOT NULL,
    raw_line NVARCHAR(MAX) NULL,
    action NVARCHAR(30) NULL,
    document NVARCHAR(30) NULL,
    email NVARCHAR(150) NULL,
    created_at_utc DATETIME2 NOT NULL,
    CONSTRAINT FK_ImportJobErrors_ImportJobs FOREIGN KEY (import_job_id) REFERENCES dbo.ImportJobs(id)
);

CREATE INDEX IX_ImportJobErrors_Job_Seq ON dbo.ImportJobErrors(import_job_id, seq);
```

#### `ImportNotifications`

```sql
CREATE TABLE dbo.ImportNotifications
(
    id INT IDENTITY(1,1) PRIMARY KEY,
    import_job_id INT NOT NULL [FK],
    user_id INT NOT NULL,
    title NVARCHAR(200) NOT NULL,
    message NVARCHAR(1000) NOT NULL,
    status NVARCHAR(20) NOT NULL,
    created_at_utc DATETIME2 NOT NULL,
    read_at_utc DATETIME2 NULL,
    CONSTRAINT FK_ImportNotifications_ImportJobs FOREIGN KEY (import_job_id) REFERENCES dbo.ImportJobs(id)
);

CREATE INDEX IX_ImportNotifications_User_Status_CreatedAt ON dbo.ImportNotifications(user_id, status, created_at_utc DESC);
```

#### `ImportJobItems` (Idempotência)

```sql
CREATE TABLE dbo.ImportJobItems
(
    id INT IDENTITY(1,1) PRIMARY KEY,
    import_job_id INT NOT NULL [FK],
    seq INT NOT NULL,
    line_hash CHAR(64) NOT NULL,
    status NVARCHAR(30) NOT NULL,
    processed_at_utc DATETIME2 NOT NULL,
    error_id INT NULL,
    target_key NVARCHAR(100) NULL,
    CONSTRAINT FK_ImportJobItems_ImportJobs FOREIGN KEY (import_job_id) REFERENCES dbo.ImportJobs(id)
);

CREATE UNIQUE INDEX UQ_ImportJobItems_Job_Seq ON dbo.ImportJobItems(import_job_id, seq);
```

### 3.2 Tabelas de Negócio Utilizadas

- `tb_empresa` (Companies) - consulta por Id e validação de acesso
- `tb_empresa_usuario` (Company User Access) - autorização por empresa
- `tb_cliente` (Clients) - INSERT/UPDATE/DELETE durante importação
- `tb_usuario` (Users) - consulta de usuário autenticado

---

## 4. Jobs e Estados Possíveis

### 4.1 Estados de Job

**Arquivo:** `Infrastructure/Import/ImportJobStatus.cs`

| Estado | Descrição | Terminal |
|--------|-----------|----------|
| `Queued` | Job criado, aguardando processamento em fila | Não |
| `Running` | Job em processamento pelo worker | Não |
| `Completed` | Todas as linhas processadas sem erros | **Sim** |
| `CompletedWithErrors` | Linhas processadas, algumas com erro | **Sim** |
| `Failed` | Nenhuma linha processada com sucesso | **Sim** |
| `CancellationRequested` | Cancelamento solicitado | Não |
| `Cancelled` | Job cancelado durante processamento | **Sim** |

### 4.2 Transições de Estado

```
Queued
  ↓ (worker pickup)
Running
  ├→ Completed (zero errors)
  ├→ CompletedWithErrors (some errors)
  ├→ Failed (all errors or parsing failure)
  └→ Cancelled (cancel request processed)

CancellationRequested
  ↓ (worker detects and processes)
Cancelled
```

### 4.3 Estados de Item (Linha)

- `Processing`: Linha sendo processada
- `Processed`: Linha processada com sucesso
- `Failed`: Linha processada com erro
- `Skipped`: Linha já processada (idempotência)

---

## 5. Notificações Existentes

### 5.1 Tipos de Notificação

| Status do Job | Título | Mensagem |
|---------------|--------|----------|
| Completed | "Importação concluída" | "A importação do arquivo {fileName} foi concluída com sucesso." |
| CompletedWithErrors | "Importação concluída com erros" | "A importação do arquivo {fileName} foi concluída com erros. Consulte os detalhes." |
| Failed | "Importação falhou" | "A importação do arquivo {fileName} falhou. Consulte os detalhes." |
| Cancelled | "Importação cancelada" | "A importação do arquivo {fileName} foi cancelada." |

### 5.2 Persistência

- Tabela: `ImportNotifications`
- Status: Unread / Read
- Lifecycle: Criada ao finalizar job, marcada como lida via endpoint

### 5.3 Endpoints de Notificação

- `GET /api/import/notifications` - listar
- `POST /api/import/notifications/{id}/read` - marcar lida
- `POST /api/import/notifications/read-all` - marcar todas como lidas

---

## 6. Histórico de Importações

### 6.1 Persistência

- Tabela: `ImportJobs` + `ImportJobErrors`
- Granularidade: Por job
- Retenção: Indefinida (sem cleanup automático)

### 6.2 Consulta de Histórico

**Endpoint:** `GET /api/import/jobs`

- Filtros: Status, DateRange, UserId (admins only)
- Paginação: Configurável (1-100 items por página)
- Ordenação: DESC por ID (mais recentes primeiro)

### 6.3 Detalhe de Importação

**Endpoint:** `GET /api/import/jobs/{jobPublicId}/errors`

- Retorna erros por linha
- Paginado (1-100 items)
- Campos capturados:
  - LineNumber
  - Action (inserir/atualizar/excluir)
  - Document (CPF mascarado em logs)
  - Email
  - Message (motivo do erro)
  - ErrorCode (NULL atualmente)
  - RawLine (capturado)

---

## 7. Funcionalidades Efetivamente Implementadas

### 7.1 Backend (.NET 8 + Minimal API + Dapper)

✅ **Importação Síncrona CSV**
- Upload multipart/form-data
- Validação de arquivo (tipo, tamanho, encoding)
- Validação de cabeçalho (9 colunas esperadas)
- Processamento linha a linha
- Contadores detalhados (total, sucesso, erro)
- Duração em ms
- Tratamento de erro por linha (não aborta lote)
- Resposta com erros detalhados

✅ **Importação Assíncrona via RabbitMQ**
- Upload com armazenamento em disco
- Publicação de mensagem
- Worker de fundo (BackgroundService)
- Processamento assíncrono
- Progress tracking a cada 20 linhas
- Heartbeat
- Suporte a cancelamento mid-processing

✅ **Validações de Negócio**
- CPF: 11 dígitos, válido (algoritmo modulo 11)
- Email: padrão RFC básico
- Data: dd/MM/yyyy ou yyyy-MM-dd
- Ação: inserir | atualizar | excluir
- Nomes: obrigatórios, até 120 chars
- Autorização: por empresa

✅ **Operações CRUD de Cliente**
- INSERT: Novo cliente
- UPDATE: Cliente existente
- DELETE/INACTIVATE: Desativar cliente

✅ **Observabilidade**
- Logs estruturados com CorrelationId
- Logs por job
- Logs por erro de linha
- Métricas: duração, contadores
- Mascaramento de CPF em logs
- Snapshot de métricas

✅ **Persistência**
- Tabelas idempotentes (CREATE TABLE IF NOT EXISTS)
- SQL parametrizado com Dapper
- Índices de performance
- Foreign keys e constraints

✅ **Histórico**
- Todos os jobs registrados
- Erros por linha registrados
- Filtros: status, daterange, userId
- Paginação

✅ **Notificações**
- Criadas ao finalizar job
- Status Unread/Read
- Tipos: Completed, CompletedWithErrors, Failed, Cancelled

### 7.2 Frontend (Angular + Material + Reactive Forms)

✅ **Upload de Arquivo**
- File input com validação de tipo
- Feedback visual de upload
- Progress bar

✅ **Preview de Dados**
- Visualização de linhas antes de processar
- Validação individual de linhas
- Seleção de linhas para processar

✅ **Processamento**
- Síncrono: resposta imediata com erros
- Assíncrono: job queued, notificação ao completar

✅ **Histórico**
- Lista de importações com paginação
- Detalhes de job
- Visualização de erros

✅ **Notificações**
- Listagem de notificações
- Marcação como lida
- Status visual

✅ **Navegação**
- Rota `/import`
- Menu "Importar"

---

## 8. Funcionalidades Parcialmente Implementadas

⚠️ **Reprocessamento de Linhas com Erro**
- Endpoint `/api/import/jobs/{jobPublicId}/retry` existe
- Lógica de reprocessamento parcial não está completa
- Necessário criar novo job a partir de erros anteriores

⚠️ **Formatação de Dados**
- Normalizações básicas implementadas (trim, case, dígitos)
- Falta tratamento de acentos, caracteres especiais em campo de texto
- Gender: Normalizado (M→male, F→female, default→not-to-say)
- BirthDate: Normalizado (dd/MM/yyyy ou yyyy-MM-dd)

⚠️ **Armazenamento de Arquivo**
- Arquivo armazenado em disco para async
- Falta limpeza automática de arquivos processados
- Falta implementação de backup/archive

⚠️ **Autorização**
- Validação por empresa implementada
- Falta validação granular por tipo de ação (admin vs user)
- Falta auditoria de quem processou qual importação

---

## 9. Funcionalidades Ainda Não Implementadas

❌ **Importação de Outras Entidades**
- Apenas clientes (clients-csv)
- Falta: empresas, usuários, outros tipos de dados
- Falta: framework genérico de importação

❌ **Importação via Integração com API externa**
- Apenas upload de arquivo CSV
- Falta: conectores com Vtex, ERP, etc
- Falta: trigger automático de importação

❌ **Validação de Negócio Avançada**
- Duplicação de CPF em outras empresas (apenas na mesma empresa)
- Consistência de email across parceiros
- Validação de departamento/cargo against catálogo
- Falta: regras customizáveis por empresa

❌ **Processamento em Lote Distribuído**
- Apenas worker único
- Falta: paralelismo de workers
- Falta: particionamento de dados

❌ **Transformação de Dados**
- Apenas normalização básica
- Falta: mapeamento de campos customizável
- Falta: enriquecimento de dados (geocoding, etc)

❌ **Reconciliação Pós-Importação**
- Sem validação de integridade após importação
- Sem comparação com dados anteriores
- Sem relatório de impacto

❌ **Agendamento de Importação**
- Apenas upload manual
- Falta: agendamento recorrente
- Falta: webhook/integration

❌ **Template de Arquivo**
- Sem download de template esperado
- Sem validação de exemplo

❌ **Importação com Soft Delete**
- Não diferencia delete de inactivate
- Falta: lógica de soft delete configurável

---

## 10. Detalhes Técnicos Importantes

### 10.1 Colunas CSV Esperadas

```
nome;sobrenome;cpf;email;sexo;dt_nascimento;departamento;cargo;acao
```

### 10.2 Validações de Coluna

| Coluna | Obrigatório | Formato | Notas |
|--------|------------|---------|-------|
| nome | Sim | Text, ≤120 | Trim |
| sobrenome | Sim | Text, ≤120 | Trim |
| cpf | Sim | 11 dígitos | Validação algorítmica |
| email | Não | RFC básico | Lowercase, trim |
| sexo | Não | M/F | Normalizado para male/female |
| dt_nascimento | Não | dd/MM/yyyy ou yyyy-MM-dd | Normalizado para yyyy-MM-dd |
| departamento | Não | Text, ≤120 | Trim |
| cargo | Não | Text, ≤120 | Trim |
| acao | Sim | inserir/atualizar/excluir | Lowercase |

### 10.3 Idempotência

- Mecanismo: Hash SHA256 da linha
- Tabela: `ImportJobItems`
- Prevenção de: duplicação de linha na mesma importação
- Não previne: duplicação entre importações diferentes

### 10.4 Segurança

- SQL Injection: Protegido via Dapper parametrizado
- CPF em Logs: Mascarado (apenas últimos 4 dígitos)
- Autorização: Validada por empresa e user
- CORS: Não configurado (verificar Web.config)

### 10.5 Performance

- Índices: Presentes em queries principales
- N+1 Queries: Mitigado com Dapper (não ORM)
- Batch Progress: Update a cada 20 linhas
- Limite de Arquivo: 5MB
- Prefetch RabbitMQ: 1 job por worker

---

## 11. Arquivos Relevantes

### Backend

- `Migracao/Partner.Api/Features/Import/ImportEndpoints.cs` - Rotas e handlers
- `Migracao/Partner.Api/Features/Import/ImportModels.cs` - DTOs e models
- `Migracao/Partner.Api/Features/Import/ImportQueries.cs` - SQL e queries
- `Migracao/Partner.Api/Infrastructure/Import/ImportWorker.cs` - Consumer RabbitMQ
- `Migracao/Partner.Api/Infrastructure/Import/ImportJobPublisher.cs` - Publisher
- `Migracao/Partner.Api/Infrastructure/Import/ImportJobStatus.cs` - Estados
- `sql_import_async_v1.sql` - DDL (schema legado)

### Frontend

- `Migracao/Partner.Web/src/app/features/import/import.page.ts` - Component
- `Migracao/Partner.Web/src/app/features/import/import.page.html` - Template
- `Migracao/Partner.Web/src/app/features/import/import.service.ts` - Service
- `Migracao/Partner.Web/src/app/features/import/import.models.ts` - DTOs

### UI Legada

- `SolutionsTools.UI/ImportarNew.aspx` - Frontend legado (vazio)
- `SolutionsTools.UI/FileUploadHandler.ashx.cs` - Upload handler legado
- `SolutionsTools.UI/FileProcessHandler.ashx.cs` - Process handler legado
- `SolutionsTools.UI/Import/*.js` - JavaScript antigo (upload.js, process.js)

### Documentação

- `Documents/plano_feature_import_csv.md` - Plano incremental
- `Documents/feature_import_csv_entrega_incremental.md` - Status de entrega
- `sql_import_async_v1.sql` - Documentação de schema

---

## Resumo Executivo

A feature Import está **funcional** com as seguintes características:

| Aspecto | Status |
|--------|--------|
| Upload de CSV | ✅ Completo (síncrono e assíncrono) |
| Validação | ✅ Básica (estrutura, CPF, email, data) |
| Processamento | ✅ Linha a linha (sem abortar lote) |
| Persistência | ✅ Idempotente com tracking |
| Observabilidade | ✅ Logs estruturados e métricas |
| RabbitMQ | ✅ Publicação e consumo implementados |
| Notificações | ✅ Criadas e consultáveis |
| Histórico | ✅ Completo com paginação |
| Frontend | ✅ Upload, preview, histórico |
| Tratamento de Erro | ✅ Por linha com detalhes |
| Cancelamento | ✅ Mid-processing |
| Reprocessamento | ⚠️ Parcial |
| Limpeza de Arquivo | ❌ Não implementado |
| Multi-tipo Importação | ❌ Apenas clientes |
| Agendamento | ❌ Não implementado |

**Próximos passos recomendados:**
1. Implementar limpeza automática de arquivos processados
2. Completar lógica de reprocessamento de erros
3. Estender para outros tipos de entidade (empresas, usuários)
4. Adicionar validações de negócio mais rigorosas
5. Implementar agendamento de importação