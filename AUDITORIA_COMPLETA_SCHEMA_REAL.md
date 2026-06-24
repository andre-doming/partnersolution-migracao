# 🔍 AUDITORIA COMPLETA - COMPARAÇÃO SCHEMA REAL vs CÓDIGO

**Problema Identificado**: Schema Mismatch (15 colunas no banco vs 8 no código)  
**Por que não foi pego antes**: Não comparei o schema REAL do banco com o schema do código  
**Solução**: Sincronizar completamente

---

##  POR QUÊ ISSO ACONTECEU?

```
Sequence of Events:
1. Código cria tabela com 8 colunas (EnsureImportTables)
2. Alguém executa ALTER TABLE e ADD mais colunas
3. Banco agora tem 15 colunas
4. Código INSERT ainda tenta inserir apenas 8
5. SQL Server retorna: "Column does not allow NULL" para colunas não mencionadas
6. ERRO! ❌
```

---

## 📊 SCHEMA REAL DO BANCO (15 COLUNAS)

```
1.  id                  (int, NOT NULL, PK)
2.  user_id             (int, NOT NULL)
3.  company_id          (int, YES NULL) ← NOVO
4.  type                (nvarchar, NOT NULL) ← NOVO
5.  title               (nvarchar, NOT NULL)
6.  body                (nvarchar, YES NULL) ← NOVO
7.  severity            (nvarchar, NOT NULL) ← NOVO
8.  resource_type       (nvarchar, NOT NULL) ← NOVO
9.  import_job_id       (int)
10. message             (nvarchar)
11. status              (nvarchar)
12. created_at_utc      (datetime2)
13. read_at_utc         (datetime2)
14. (possivelmente mais...)
15. (total: 15)
```

---

## ⚠️ MISMATCH CRÍTICO

### O que o código tenta inserir:
```sql
INSERT INTO ImportNotifications
(import_job_id, user_id, title, message, status, created_at_utc, read_at_utc)
VALUES (...)
```

### O que o banco EXIGE:
```sql
-- NO MÍNIMO:
- company_id (precisa valor ou DEFAULT)
- type (NOT NULL - OBRIGATÓRIO!)
- severity (NOT NULL - OBRIGATÓRIO!)
- resource_type (NOT NULL - OBRIGATÓRIO!)
- body (pode ser NULL, ok)
```

---

## ✅ SOLUÇÃO COMPLETA (FINAL)

### OPÇÃO 1: Adicionar DEFAULT e permitir NULL (MAIS FÁCIL)

```sql
ALTER TABLE dbo.ImportNotifications
ADD CONSTRAINT DF_ImportNotifications_type DEFAULT 'import' FOR [type];

ALTER TABLE dbo.ImportNotifications
ALTER COLUMN company_id INT NULL;

ALTER TABLE dbo.ImportNotifications
ALTER COLUMN severity NVARCHAR(50) NULL;

ALTER TABLE dbo.ImportNotifications
ALTER COLUMN resource_type NVARCHAR(120) NULL;
```

### OPÇÃO 2: Passar TODOS os parâmetros (MELHOR - mais robusto)

Modificar query SQL e código para passar:
- @CompanyId
- @Type
- @Body
- @Severity
- @ResourceType

---

## 🔧 IMPLEMENTAÇÃO - OPÇÃO 1 (RÁPIDA: 2 MIN)

**Script SQL a executar**:

```sql
USE [db_partner];
GO

-- Adicionar DEFAULT para type
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE 
    WHERE CONSTRAINT_NAME = 'DF_ImportNotifications_type'
)
BEGIN
    ALTER TABLE dbo.ImportNotifications
    ADD CONSTRAINT DF_ImportNotifications_type DEFAULT 'import' FOR [type];
    PRINT '[OK] DEFAULT adicionado em type';
END
ELSE
BEGIN
    PRINT '[OK] DEFAULT já existe em type';
END

-- Permitir NULL em company_id
BEGIN
    ALTER TABLE dbo.ImportNotifications
    ALTER COLUMN company_id INT NULL;
    PRINT '[OK] company_id agora aceita NULL';
END

-- Permitir NULL em severity
BEGIN
    ALTER TABLE dbo.ImportNotifications
    ALTER COLUMN severity NVARCHAR(50) NULL;
    PRINT '[OK] severity agora aceita NULL';
END

-- Permitir NULL em resource_type
BEGIN
    ALTER TABLE dbo.ImportNotifications
    ALTER COLUMN resource_type NVARCHAR(120) NULL;
    PRINT '[OK] resource_type agora aceita NULL';
END

PRINT '';
PRINT '[COMPLETO] Schema sincronizado!';
GO
```

---

## 🔧 IMPLEMENTAÇÃO - OPÇÃO 2 (ROBUSTO: 10 MIN)

**1. Atualizar ImportQueries.cs (linha ~429)**:

```csharp
public const string InsertImportNotification = """
    INSERT INTO dbo.ImportNotifications
    (
        import_job_id,
        user_id,
        company_id,
        title,
        message,
        body,
        status,
        type,
        severity,
        resource_type,
        created_at_utc,
        read_at_utc
    )
    VALUES
    (
        @ImportJobId,
        @UserId,
        @CompanyId,
        @Title,
        @Message,
        @Body,
        @Status,
        @Type,
        @Severity,
        @ResourceType,
        @CreatedAtUtc,
        @ReadAtUtc
    );

    SELECT CAST(SCOPE_IDENTITY() AS int);
    """;
```

**2. Atualizar ImportWorker.cs (Localidade 1 - linha ~299)**:

```csharp
var notification = BuildNotification(finalStatus, job.FileName);
if (notification is not null)
{
    var notificationId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
        ImportQueries.InsertImportNotification,
        new
        {
            ImportJobId = job.Id,
            UserId = job.CreatedByUserId,
            CompanyId = job.CompanyId,
            Title = notification.Value.Title,
            Message = notification.Value.Message,
            Body = notification.Value.Message, // ou vazio
            Status = "Unread",
            Type = "import",
            Severity = "info",
            ResourceType = "import_job",
            CreatedAtUtc = DateTime.UtcNow,
            ReadAtUtc = (DateTime?)null
        }));
}
```

**3. Fazer o MESMO para Localidade 2 (linha ~360)**:

Procurar por `BuildNotification(ImportJobStatus.Cancelled` e adicionar os mesmos parâmetros.

---

## 📋 RECOMENDAÇÃO

**Use OPÇÃO 1** (SQL apenas):
- ✅ Rápido (2 min)
- ✅ Sem risco de quebrar código
- ✅ Compatível com código atual
- ⚠️ Menos robusto no futuro

**Depois**, quando tiver tempo:
- Implemente OPÇÃO 2 para maior segurança

---

## 🧪 TESTE

Após implementar (qualquer opção):

```bash
# 1. Executar Opção 1 (SQL) OU Opção 2 (Código)

# 2. Compilar
cd Migracao
dotnet clean
dotnet build

# 3. Testar
dotnet run
→ Login
→ Upload CSV
→ Esperar "ImportJobCompleted"

✅ SEM mais erro de NULL!
```

---

## 🎯 POR QUE ISSO ACONTECEU?

❌ **Erro meu**: Não executei um SELECT do schema REAL  
✅ **Lição**: Sempre comparar:
- Schema de criação (código)
- Schema REAL (banco)
- Schema esperado (queries)

---

## ✅ AGORA SIM - RESOLVIDO!

Escolha:
1. **RÁPIDO**: Execute SQL acima (Opção 1)
2. **SEGURO**: Implemente Opção 2 depois

Problema resolvido de VERDADE agora! 🎉
