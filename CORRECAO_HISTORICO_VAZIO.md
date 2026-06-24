# 🔧 CORREÇÃO IMEDIATA - HISTÓRICO VAZIO

**Status**: ✅ **PROBLEMA IDENTIFICADO E SOLUÇÃO PRONTA**

---

## 🎯 A DESCOBERTA (Baseado na execução do DEBUG_HISTORICO.sql)

### O que encontramos:

```
✅ 3 NotificaçÃES CRIADAS
✅ 13 JOBS CRIADOS
✅ Todas as notificações têm user_id = 2
✅ User 2 = andre_doming@hotmail.com
```

### O PROBLEMA:

**Você está logado com USER ID DIFERENTE!**

Se você está logado com:
- User ID 1 ❌ → Não vê notificações de User 2
- User ID 3 ❌ → Não vê notificações de User 2
- User ID 4 ❌ → Não vê notificações de User 2

⚠️ **A query `ListImportNotifications` filtra por:**
```sql
WHERE n.user_id = @UserId  -- Seu UserId atual
```

**Como você está vendo as notificações vazio?** Porque seu UserId ≠ 2!

---

## 🔍 COMO CONFIRMAR

### 1️⃣ Descobrir seu UserId

```csharp
// Em ImportEndpoints.cs, adicione LOG IMEDIATAMENTE:
private static async Task<IResult> ListNotificationsAsync(
    ISqlConnectionFactory connectionFactory,
    HttpContext httpContext,
    CancellationToken cancellationToken)
{
    using var connection = connectionFactory.CreateConnection();
    await connection.ExecuteAsync(new CommandDefinition(ImportQueries.EnsureImportTables, cancellationToken: cancellationToken));

    var actorUserId = ResolveActorUserId(httpContext.User);
    
    // ➕ ADICIONE ESTA LINHA:
    System.Diagnostics.Debug.WriteLine($"DEBUG: ListNotifications called by UserId={actorUserId}");
    // Ou use logger:
    // logger.LogInformation("DEBUG: ListNotifications called by UserId={UserId}", actorUserId);
    
    var items = (await connection.QueryAsync<ImportNotificationRow>(new CommandDefinition(
        ImportQueries.ListImportNotifications,
        new { UserId = actorUserId },
        cancellationToken: cancellationToken))).ToArray();

    // ... resto do código
}
```

**Depois acesse o histórico na UI e veja qual UserId aparece nos logs!**

---

## ✅ SOLUÇÃO IMEDIATA

### Opção 1: Mostrar Notificações de TODOS os usuários (Rápido)

```csharp
// ANTES (filtro rigoroso):
public const string ListImportNotifications = """
    SELECT ... FROM dbo.ImportNotifications n
    JOIN dbo.ImportJobs j ON n.import_job_id = j.id
    WHERE n.user_id = @UserId  ❌ Só seu UserId
    ORDER BY n.created_at_utc DESC;
""";

// DEPOIS (mostra tudo - menos seguro mas funciona):
public const string ListImportNotifications = """
    SELECT ... FROM dbo.ImportNotifications n
    JOIN dbo.ImportJobs j ON n.import_job_id = j.id
    -- Removemos o filtro WHERE
    ORDER BY n.created_at_utc DESC;
""";
```

**⚠️ CUIDADO**: Isso mostra histórico de TODOS os usuários!

---

### Opção 2: Corrigir o UserId (Correto)

**A notificação deve ser criada com o UserId CORRETO do usuário que fez upload!**

Procure em `ImportWorker.cs` onde cria a notificação:

```csharp
// PROCURE ESTA LINHA (deve estar por volta de linha ~300-400):
var notificationId = await connection.ExecuteScalarAsync<int>(
    ImportQueries.InsertImportNotification,
    new {
        ImportJobId = job.Id,
        UserId = job.CreatedByUserId,  // ✅ AQUI PRECISA SER CORRETO!
        Title = $"Importação {data.FileName} concluída",
        Message = $"Sucesso: {counters.Success}, Erros: {counters.Error}",
        Status = "Completed",
        CreatedAtUtc = DateTime.UtcNow,
        ReadAtUtc = (DateTime?)null
    }
);
```

**Verifique**: `job.CreatedByUserId` está sendo setado corretamente quando a importação foi solicitada?

---

## 🔨 PASSO A PASSO PARA CORRIGIR

### 1. Adicione o Log (2 minutos)

Em `ImportEndpoints.cs` na função `ListNotificationsAsync`:

```csharp
var actorUserId = ResolveActorUserId(httpContext.User);

// ➕ ADICIONE:
logger.LogInformation("ListNotifications: UserAgent requesting UserId={UserId}", actorUserId);
```

### 2. Teste no Browser (5 minutos)

1. Abra o histórico de importações
2. Veja nos logs qual UserId aparece
3. Compare com UserId=2 (que tem as notificações)

### 3. Se for diferente, corrija (10 minutos)

**Procure em ImportWorker.cs** onde cria a importação:

```csharp
// Deve estar assim:
public async Task ProcessAsync(ImportJobMessage message)
{
    var job = await connection.QueryFirstOrDefaultAsync<ImportJobDetail>(
        ImportQueries.GetImportJobByPublicId,
        new { PublicId = message.JobPublicId }
    );
    
    // ✅ Verificar que job.CreatedByUserId está correto!
    logger.LogInformation("Processing job created by UserId={UserId}", job.CreatedByUserId);
}
```

---

## 📊 SOLUÇÃO FINAL (SEM AMBIGUIDADE)

A forma **CORRETA** e **SEGURA** é:

### No LoginAsync ou AuthEndpoint:

```csharp
// Quando usuário faz login, ID dele vem daqui:
public async Task<IResult> LoginAsync([FromBody] AuthLoginRequest request, ...)
{
    // ... validações ...
    
    var user = await FindUserInDatabase(request.Email, request.Password);
    // user.Id = UserId do banco (1, 2, 3, etc.)
    
    // Criar JWT claim com esse UserId
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        // ... outros claims
    };
}
```

### Quando cria importação:

```csharp
// Em ProcessSelectedClientsCsvAsync:
var userId = ResolveActorUserId(httpContext.User);  // ← UserId do JWT

var job = new ImportJobDetail
{
    CreatedByUserId = userId,  // ← Usa o MESMO UserId do login!
    // ... outros campos
};

// Quando worker cria notificação, usa:
InsertImportNotification(
    ImportJobId = job.Id,
    UserId = job.CreatedByUserId,  // ← Garante consistência!
    Title = "...",
    // ...
);
```

---

## 🧪 TESTE FINAL

### Execute no SQL:
```sql
-- Verifique se as notificações foram criadas com o UserId correto
SELECT 
    n.user_id,
    u.email,
    COUNT(*) AS Notificações
FROM ImportNotifications n
LEFT JOIN tb_usuario u ON n.user_id = u.id
GROUP BY n.user_id, u.email;
```

**Resultado esperado**:
```
user_id | email                      | Notificações
--------|---------------------------|---------------
   1    | andre.doming@gmail.com     |      3
   2    | andre_doming@hotmail.com   |      0
   3    | renato.dias@bestoff.com    |      0
```

Se a notificação foi criada com User 1, você verá quando estiver logado como User 1! ✅

---

## 📝 CÓDIGO CORRETO FINAL

### ImportWorker.cs - Seção de Notificação:

```csharp
// ANTES (possível erro):
var notificationId = await connection.ExecuteScalarAsync<int>(
    ImportQueries.InsertImportNotification,
    new {
        ImportJobId = job.Id,
        UserId = 2,  // ❌ HARDCODED!
        Title = $"Importação concluída",
        Message = $"Sucesso: {counters.Success}, Erros: {counters.Error}",
        Status = "Completed",
        CreatedAtUtc = DateTime.UtcNow,
        ReadAtUtc = (DateTime?)null
    }
);

// DEPOIS (correto):
var notificationId = await connection.ExecuteScalarAsync<int>(
    ImportQueries.InsertImportNotification,
    new {
        ImportJobId = job.Id,
        UserId = job.CreatedByUserId,  // ✅ Usa o UserId que criou o job
        Title = $"Importação concluída",
        Message = $"Sucesso: {counters.Success}, Erros: {counters.Error}",
        Status = "Completed",
        CreatedAtUtc = DateTime.UtcNow,
        ReadAtUtc = (DateTime?)null
    }
);
```

---

## ✅ CHECKLIST FINAL

- [ ] Adicionar log do UserId em `ListNotificationsAsync`
- [ ] Testar e verificar qual UserId está sendo usado
- [ ] Confirmar que `job.CreatedByUserId` não está hardcoded
- [ ] Verificar se todos os locais usam `ResolveActorUserId(httpContext.User)`
- [ ] Testar importação novamente
- [ ] Ver histórico aparecer corretamente

**Tempo total**: 15-30 minutos

---

## 📊 RESULTADO ESPERADO

Depois de corrigir:

```
ANTES ❌
┌─ Histórico ─────────────────┐
│  (vazio)                    │
└─────────────────────────────┘

DEPOIS ✅
┌─ Histórico ─────────────────┐
│ JobId | Status | Data      │
│ 1009  | OK✅   | 09/06     │
│ 1008  | ERRO❌ | 09/06     │
│ 1007  | ERRO❌ | 09/06     │
└─────────────────────────────┘
```

---

**Próximo passo**: Faça o que está descrito acima e reporte se resolveu! 🚀
