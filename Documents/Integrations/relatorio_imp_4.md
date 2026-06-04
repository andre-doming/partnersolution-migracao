# IMP-4 — Notificações Persistidas de Importação

## Resumo
Implementadas notificações persistidas com polling para informar conclusão de importações (Completed, CompletedWithErrors, Failed, Cancelled). Inclui backend (tabela, endpoints, logs), frontend (badge, painel, toast, polling) e testes. **Sem SignalR/WebSockets/Push/Email/SMS/VTEX/Retry/Cancelamento**, conforme escopo.

---

## 1) Banco de dados

Tabela utilizada: **ImportNotifications** (ajustada para o modelo mínimo do IMP-4).

Campos:

- `Id`
- `ImportJobId`
- `UserId`
- `Title`
- `Message`
- `Status` (Unread / Read)
- `CreatedAtUtc`
- `ReadAtUtc`

Atualizado em:

- `Migracao/Partner.Api/Features/Import/ImportQueries.cs` (DDL + queries)
- `sql_import_async_v1.sql` (script versionado)

---

## 2) Geração das notificações

### Backend (Worker + Sync Import)

Criadas notificações ao final do processamento:

| Status | Título | Mensagem |
| --- | --- | --- |
| Completed | Importação concluída | A importação do arquivo {FileName} foi concluída com sucesso. |
| CompletedWithErrors | Importação concluída com erros | A importação do arquivo {FileName} foi concluída com erros. Consulte os detalhes. |
| Failed | Importação falhou | A importação do arquivo {FileName} falhou. Consulte os detalhes. |
| Cancelled | Importação cancelada | A importação do arquivo {FileName} foi cancelada. |

Arquivos:

- `Migracao/Partner.Api/Infrastructure/Import/ImportWorker.cs`
- `Migracao/Partner.Api/Features/Import/ImportEndpoints.cs` (sync)

---

## 3) Endpoints (backend)

Endpoints criados:

- **GET** `/api/import/notifications`
  - Retorna: `Id`, `Title`, `Message`, `Status`, `CreatedAtUtc`, `ImportJobPublicId`

- **POST** `/api/import/notifications/{id}/read`
  - Marca uma notificação como lida.

- **POST** `/api/import/notifications/read-all`
  - Marca todas como lidas.

Segurança:

- Usuário comum e Admin veem **somente suas notificações**.
- Sem painel global nesta fase.

---

## 4) Observabilidade

Eventos adicionados:

- `NotificationCreated`
- `NotificationRead`
- `NotificationReadAll`

Campos nos logs:

- `CorrelationId`
- `JobId`
- `JobPublicId`
- `UserId`

Arquivos:

- `Migracao/Partner.Api/Infrastructure/Import/ImportWorker.cs`
- `Migracao/Partner.Api/Features/Import/ImportEndpoints.cs`

---

## 5) Frontend

### Badge de notificações

- Ícone no layout principal com badge de não lidas.
- Atualizado por polling (30s).

Arquivos:

- `Migracao/Partner.Web/src/app/layout/shell/shell.component.html`
- `Migracao/Partner.Web/src/app/layout/shell/shell.component.ts`
- `Migracao/Partner.Web/src/app/layout/shell/shell.component.scss`

### Painel de notificações

- Listagem de notificações, data, status, botão de marcar como lida e “Visualizar”.
- Ação de “marcar todas como lidas”.

Arquivos:

- `Migracao/Partner.Web/src/app/layout/shell/notifications-panel/*`

### Toast automático

- Ao identificar nova notificação via polling, exibe toast discreto com ação “Visualizar”.

### Navegação para detalhe

- Ao clicar em “Visualizar”, navega para `/import?job={publicId}`.
- Import page carrega detalhes automaticamente via query param.

Arquivos:

- `Migracao/Partner.Web/src/app/features/import/import.page.ts`

---

## 6) Polling

Estratégia implementada:

- **30 segundos**
- Atualiza badge, lista e detecta novas notificações para toast

Arquivos:

- `Migracao/Partner.Web/src/app/layout/shell/shell.component.ts`

---

## 7) Testes executados

### Backend

```
dotnet build Partner.Modern.sln
dotnet test Partner.Modern.sln
```

Resultado:

- Build OK (1 warning pré-existente)
- Tests OK: 34

### Frontend

```
cd Migracao/Partner.Web
npm test -- --watch=false
npm run build
```

Resultado:

- Tests OK: 7
- Build OK (1 warning de budget inicial excedido)

---

## 8) Limitações da fase (mantidas)

- Sem SignalR/WebSockets/Push.
- Sem e-mail/SMS.
- Sem integração VTEX.
- Sem retry manual.
- Sem cancelamento.

---

## Arquivos principais alterados

**Backend**

- `Migracao/Partner.Api/Features/Import/ImportQueries.cs`
- `Migracao/Partner.Api/Features/Import/ImportEndpoints.cs`
- `Migracao/Partner.Api/Infrastructure/Import/ImportWorker.cs`
- `Migracao/Partner.Api/Features/Import/ImportModels.cs`
- `sql_import_async_v1.sql`
- `Migracao/Partner.Api.Tests/Import/ImportAsyncFoundationTests.cs`

**Frontend**

- `Migracao/Partner.Web/src/app/features/import/import.models.ts`
- `Migracao/Partner.Web/src/app/features/import/import.service.ts`
- `Migracao/Partner.Web/src/app/layout/shell/shell.component.ts`
- `Migracao/Partner.Web/src/app/layout/shell/shell.component.html`
- `Migracao/Partner.Web/src/app/layout/shell/shell.component.scss`
- `Migracao/Partner.Web/src/app/layout/shell/notifications-panel/*`
- `Migracao/Partner.Web/src/app/features/import/import.page.ts`
- `Migracao/Partner.Web/src/app/features/import/import.page.spec.ts`

---

## Próximos passos sugeridos

- Evoluir notificações com SignalR quando permitido.
- Ajustar budget de bundle Angular.
- Refino do painel (filtros, paginação).

***

**Documento IMP-4 finalizado.**