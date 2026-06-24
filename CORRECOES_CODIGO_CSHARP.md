# 🔧 CORREÇÕES DE CÓDIGO C# - PASSO-A-PASSO EXATO

**Objetivo**: Adicionar suporte à coluna `type` em ImportNotifications  
**Tempo**: 5 minutos  
**Resultado**: Importação funciona 100%

---

## ✅ CORREÇÃO 1: ImportQueries.cs (Linha ~429)

**Arquivo**: `Migracao/Partner.Api/Features/Import/ImportQueries.cs`

### ANTES:
```csharp
public const string InsertImportNotification = """
    INSERT INTO dbo.ImportNotifications
    (
        import_job_id,
        user_id,
        title,
        message,
        status,
        created_at_utc,
        read_at_utc
    )
    VALUES
    (
        @ImportJobId,
        @UserId,
        @Title,
        @Message,
        @Status,
        @CreatedAtUtc,
        @ReadAtUtc
    );

    SELECT CAST(SCOPE_IDENTITY() AS int);
    """;
```

### DEPOIS:
```csharp
public const string InsertImportNotification = """
    INSERT INTO dbo.ImportNotifications
    (
        import_job_id,
        user_id,
        title,
        message,
        status,
        type,
        created_at_utc,
        read_at_utc
    )
    VALUES
    (
        @ImportJobId,
        @UserId,
        @Title,
        @Message,
        @Status,
        @Type,
        @CreatedAtUtc,
        @ReadAtUtc
    );

    SELECT CAST(SCOPE_IDENTITY() AS int);
    """;
```

**Mudanças**:
- ✅ Linha 6: Adicionar `type,` 
- ✅ Linha 17: Adicionar `@Type,`

---

## ✅ CORREÇÃO 2: ImportWorker.cs (Primeira localidade - Linha ~299)

**Arquivo**: `Migracao/Partner.Api/Infrastructure/Import/ImportWorker.cs`

**Procure por**: `BuildNotification(finalStatus`

### ANTES:
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
            Title = notification.Value.Title,
            Message = notification.Value.Message,
            Status = "Unread",
            CreatedAtUtc = DateTime.UtcNow
        }));
}
```

### DEPOIS:
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
            Title = notification.Value.Title,
            Message = notification.Value.Message,
            Status = "Unread",
            Type = "import",
            CreatedAtUtc = DateTime.UtcNow
        }));
}
```

**Mudanças**:
- ✅ Adicionar nova linha: `Type = "import",`
- ✅ Colocar ANTES de `CreatedAtUtc`

---

## ✅ CORREÇÃO 3: ImportWorker.cs (Segunda localidade - Linha ~360)

**Arquivo**: `Migracao/Partner.Api/Infrastructure/Import/ImportWorker.cs`

**Procure por**: `BuildNotification(ImportJobStatus.Cancelled`

### ANTES:
```csharp
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
            Status = "Unread",
            CreatedAtUtc = DateTime.UtcNow
        }));
}
```

### DEPOIS:
```csharp
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
            Status = "Unread",
            Type = "import",
            CreatedAtUtc = DateTime.UtcNow
        }));
}
```

**Mudanças**:
- ✅ Adicionar nova linha: `Type = "import",`
- ✅ Colocar ANTES de `CreatedAtUtc`

---

## 📋 CHECKLIST DE EXECUÇÃO

```
PASSO 1: SQL Server
☐ Abrir SQL Server Management Studio
☐ Conectar em db_partner
☐ Abrir arquivo: SCRIPT_CORRECAO_DEFINITIVA.sql
☐ Executar script completo (F5)
☐ Verificar output: "[COMPLETO] Script de Correção Executado com Sucesso!"

PASSO 2: VS Code - ImportQueries.cs
☐ Abrir: Migracao/Partner.Api/Features/Import/ImportQueries.cs
☐ Procurar (Ctrl+F): "public const string InsertImportNotification"
☐ Localizar linha ~429
☐ Adicionar "type," e "@Type," conforme acima

PASSO 3: VS Code - ImportWorker.cs (Localidade 1)
☐ Abrir: Migracao/Partner.Api/Infrastructure/Import/ImportWorker.cs
☐ Procurar (Ctrl+F): "BuildNotification(finalStatus"
☐ Localizar linha ~299
☐ Adicionar "Type = "import"," antes de "CreatedAtUtc"

PASSO 4: VS Code - ImportWorker.cs (Localidade 2)
☐ No mesmo arquivo
☐ Procurar (Ctrl+F): "BuildNotification(ImportJobStatus.Cancelled"
☐ Localizar linha ~360
☐ Adicionar "Type = "import"," antes de "CreatedAtUtc"

PASSO 5: Compilar
☐ Abrir Terminal: Ctrl + ` (backtick)
☐ cd Migracao
☐ dotnet clean
☐ dotnet build

✓ Se BUILD SUCCESSFUL → Próximo passo
✗ Se BUILD FAILED → Verificar erros de sintaxe

PASSO 6: Testar
☐ dotnet run
☐ Aguardar "[*] Starting Partner.Api server..."
☐ Fazer novo login
☐ Fazer upload CSV
☐ Aguardar " ImportJobCompleted"
☐ Verificar que NÃO há erro de "type"
```

---

## 🧪 RESULTADO ESPERADO

Após aplicar todas as correções, você DEVE ver:

```
[22:25:00 INF] ImportJobStarted JobId=1005 ...
[22:25:01 INF] ImportRowProcessed LineNumber=1 ...
[22:25:01 INF] ImportRowProcessed LineNumber=2 ...
...
[22:25:10 INF] ImportJobCompleted JobId=1005 Status=Completed
[22:25:10 INF] ImportNotificationCreated Title="Importação concluída com sucesso"
✓ SUCESSO!
```

E NUNCA mais este erro:
```
❌ Cannot insert the value NULL into column 'type'
```

---

## 🚨 ERROS COMUNS

### Erro 1: Syntax Error ao compilar
```
Solução: Verificar aspas e vírgulas no ImportQueries.cs
Procure por: linha com @Type, deve estar dentro do VALUES
```

### Erro 2: Build falha em ImportWorker.cs
```
Solução: Verificar que Type = "import", está dentro do anonymous object {}
Procure por: Type = "import", deve estar antes de CreatedAtUtc
```

### Erro 3: Ainda receba erro de NULL
```
Solução: Verificar que o script SQL foi executado
Execute novamente: SCRIPT_CORRECAO_DEFINITIVA.sql
Depois recompile o código
```

---

## ✅ PRONTO! 

Agora você tem:
- ✅ Script SQL para corrigir banco
- ✅ 3 correções de código exatas
- ✅ Checklist para executar
- ✅ Resultado esperado para validar

**SEM MAIS SURPRESAS!** 🎉
