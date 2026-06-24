# 🔍 AUDITORIA COMPLETA - VALIDAÇÃO DE CRUD E INTEGRIDADE DO SISTEMA

**Data**: 06/08/2026  
**Status**: ❌ CRÍTICO - Múltiplos Erros de Integridade Encontrados

---

## 📋 RESUMO EXECUTIVO

O sistema possui **3 erros críticos** que impedem a importação de clientes:

1. ❌ **Parâmetro @DocumentHash não declarado** (linha 738-749 ImportWorker.cs)
2. ❌ **Coluna import_job_id ausente** na tabela ImportNotifications 
3. ❌ **Mismatch de parâmetros** entre código e queries

---

## 🔴 ERRO #1: @DocumentHash NÃO DECLARADO

### Localização
- **Arquivo**: `Migracao/Partner.Api/Infrastructure/Import/ImportWorker.cs`
- **Linhas**: 738-749
- **Método**: `ProcessLineAsync()`

### Problema
```csharp
// ❌ CÓDIGO ATUAL (ERRADO)
await connection.ExecuteAsync(new CommandDefinition(
    ImportQueries.InsertClient,
    new
    {
        data.FirstName,      // ✅ OK
        data.LastName,       // ✅ OK
        data.Document,       // ❌ WRONG: Query espera @DocumentHash, não @Document
        data.Email,          // ✅ OK
        Registration = BuildRegistration(data),  // ✅ OK
        company.PartnerId,   // ✅ OK
        ClientGuid = Guid.NewGuid()  // ✅ OK
    },
    cancellationToken: cancellationToken));
```

### Query Esperada (ImportQueries.cs, linhas 328-351)
```sql
INSERT INTO tb_cliente (nome, sobrenome, cpf, email, registro_colaborador, id_parceiro, ativo, id_cliente)
VALUES (@FirstName, @LastName, @DocumentHash, @Email, @Registration, @PartnerId, 'S', @ClientGuid);
                                ^^^^^^^^^^^^ ← Query espera este nome!
```

### MAS O CÓDIGO PASSA
```
@Document ← Mismatch!
```

### Impacto
```
❌ Must declare the scalar variable "@DocumentHash"
   (Este erro aparece em TODOS os registros importados)
```

### Solução
O parâmetro precisa ser renomeado de `@Document` para `@DocumentHash` OU a query precisa receber o documento criptografado.

---

## 🔴 ERRO #2: COLUNA import_job_id AUSENTE

### Localização
- **Arquivo**: `Migracao/Partner.Api/Features/Import/ImportEndpoints.cs`
- **Linha**: 115
- **Método**: `ListNotificationsAsync()`

### Problema
Ao executar:
```csharp
var items = (await connection.QueryAsync<ImportNotificationRow>(new CommandDefinition(
    ImportQueries.ListImportNotifications,  // ← Query que usa import_job_id
    ...
```

### Query Executada (ImportQueries.cs, linhas 554-569)
```sql
SELECT
    n.id AS Id,
    n.import_job_id AS ImportJobId,  -- ← COLUNA NÃO EXISTE!
    ...
FROM dbo.ImportNotifications n
JOIN dbo.ImportJobs j ON n.import_job_id = j.id
```

### Erro Executado
```
Invalid column name 'import_job_id'.
Error: 207, Class: 16
```

### Verificação de Schema (Esperado vs Real)
Na definição de tabela (ImportQueries.cs, linhas 122-134):
```sql
CREATE TABLE dbo.ImportNotifications (
    id INT IDENTITY(1,1) PRIMARY KEY,
    import_job_id INT NOT NULL,  -- ← Deveria existir
    user_id INT NOT NULL,
    title NVARCHAR(200) NOT NULL,
    message NVARCHAR(MAX) NOT NULL,
    status NVARCHAR(30) NOT NULL,
    created_at_utc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    read_at_utc DATETIME2 NULL,
    CONSTRAINT FK_ImportNotifications_ImportJobs FOREIGN KEY (import_job_id) REFERENCES dbo.ImportJobs(id)
);
```

### Causa Raiz
O banco de dados não foi criado/migrado corretamente. A coluna `import_job_id` está na definição SQL mas não foi criada no banco real.

### Impacto
```
❌ Endpoint GET /api/import/notifications retorna 500
❌ Impossível listar notificações de importação
```

---

## 🟡 PROBLEMA #3: MISMATCH DE PARÂMETROS

### Update Client (linhas 759-776 ImportWorker.cs)
```csharp
// ❌ PROBLEMA: Passa @Document mas query espera @DocumentHash
await connection.ExecuteAsync(new CommandDefinition(
    ImportQueries.UpdateClient,
    new
    {
        existing.Id,
        data.FirstName,
        data.LastName,
        data.Document,  // ← Mismatch!
        data.Email,
        Registration = BuildRegistration(data),
        company.PartnerId
    },
    cancellationToken: cancellationToken));
```

### Query (ImportQueries.cs, linhas 353-364)
```sql
UPDATE tb_cliente
SET
    nome = @FirstName,
    sobrenome = @LastName,
    cpf = @DocumentHash,  -- ← Query espera este nome
    email = @Email,
    registro_colaborador = @Registration,
    id_parceiro = @PartnerId,
    ativo = 'S'
WHERE id = @Id;
```

---

## 🟠 PROBLEMAS SECUNDÁRIOS

### 1. Warning: VtexClient.cs linha 225
```
CS1998: This async method lacks 'await' operators and will run synchronously.
Arquivo: Migracao/Partner.Api/Infrastructure/Integrations/Vtex/VtexClient.cs
```

### 2. Null Safety Issues em Testes
```
CS8767: A nulidade de tipos de referência...
CS8600: Conversão de literal nula...
CS8604: Possível argumento de referência nula...
Arquivo: Migracao/Partner.Api.Tests/...
```

---

## 📊 TABELA DE VERIFICAÇÃO DE CRUD

| Operação | Tabela | Status | Descrição |
|----------|--------|--------|-----------|
| CREATE (Insert Client) | tb_cliente | ❌ ERRO | Parâmetro @DocumentHash não mapeado |
| READ (Find Client) | tb_cliente | ✅ OK | Query correta em FindActiveClientByDocumentOrEmail |
| UPDATE (Update Client) | tb_cliente | ❌ ERRO | Parâmetro @DocumentHash não mapeado |
| DELETE (Inactivate Client) | tb_cliente | ✅ OK | Query correta em InactivateClient |
| CREATE (Insert ImportJob) | ImportJobs | ✅ OK | Todos os parâmetros mapeados |
| READ (Get ImportJob) | ImportJobs | ✅ OK | Queries corretas |
| UPDATE (Update ImportJob) | ImportJobs | ✅ OK | Todos os parâmetros mapeados |
| CREATE (Insert ImportNotification) | ImportNotifications | ⚠️ AVISO | Coluna import_job_id pode não existir |
| READ (List Notifications) | ImportNotifications | ❌ ERRO | Coluna import_job_id ausente |
| UPDATE (Mark Read) | ImportNotifications | ⚠️ AVISO | Coluna import_job_id pode não existir |

---

## 🛠️ COMO O WORKER FUNCIONA

### Arquitetura
```
┌─────────────────────────────────────────────────────────────┐
│                      Worker Flow                             │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  1. RabbitMQ Queue (ImportJobsQueue)                        │
│     ↓                                                        │
│  2. OnMessageReceivedAsync() - Processa mensagem             │
│     ↓                                                        │
│  3. GetByPublicIdAsync() - Busca job no BD                   │
│     ↓                                                        │
│  4. ProcessJob() - Processa CSV linha por linha            │
│     ├─ ValidateHeader()                                     │
│     ├─ Para cada linha:                                     │
│     │  ├─ ProcessLineWithIdempotencyAsync()                │
│     │  └─ ProcessLineAsync() (INSERT/UPDATE/DELETE)         │
│     └─ UpdateStatus()                                       │
│     ↓                                                        │
│  5. Salva resultado no BD                                    │
│     ↓                                                        │
│  6. Envia notificação para usuário                          │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Locais chave

**Inicialização Worker**: `ImportWorker.cs` linhas 29-70
- Conecta RabbitMQ
- Declara exchange e filas
- Inicia consumer assíncrono

**Message Handler**: `ImportWorker.cs` linhas 72-140
- Deserializa mensagem JSON
- Cria scope de DI
- Chama ProcessJobAsync()

**Processamento CSV**: `ImportWorker.cs` linhas 330-450
- Valida header do CSV
- Processa cada linha
- Salva errors e progresso

**Execução de Ação (Insert/Update/Delete)**: `ImportWorker.cs` linhas 720-789
```
┌─ Ação = "inserir"  → INSERT novo cliente
├─ Ação = "atualizar" → UPDATE cliente existente
└─ Ação = "excluir"  → UPDATE para inativo
```

**Idempotência**: `ImportWorker.cs` linhas 400-440
- Verifica se linha já foi processada
- Usa linha_hash para garantir que não processa duplicadas

---

## ✅ CHECKLIST DE CORREÇÃO NECESSÁRIA

- [ ] **CRÍTICO**: Corrigir mapping de @DocumentHash em Insert/Update de clientes
- [ ] **CRÍTICO**: Executar EnsureImportTables para criar coluna import_job_id
- [ ] **CRÍTICO**: Validar que todas as colunas existem no banco de dados
- [ ] IMPORTANTE: Remover warnings de async/await
- [ ] IMPORTANTE: Corrigir null safety em testes
- [ ] RECOMENDADO: Criar procedure para validar integridade de dados
- [ ] RECOMENDADO: Adicionar testes de integração para CRUD

---

## 🔐 PRÓXIMOS PASSOS

1. **Imediato**: Executar correções de código
2. **Imediato**: Executar migration de banco de dados
3. **Curto prazo**: Testar importação de clientes
4. **Curto prazo**: Validar que todos os endpoints funcionam
5. **Médio prazo**: Adicionar testes automatizados para evitar regressão


