# Importação Assíncrona V2 — Desenho Arquitetural Final (RabbitMQ)

> **Status:** aprovado para implementação (sem código neste documento).  
> **Objetivo:** consolidar o desenho arquitetural definitivo da importação assíncrona, com RabbitMQ e observabilidade, mantendo VTEX desacoplado.

---

## 0) Premissas aprovadas

### RabbitMQ
- **Host parametrizável**
- **Porta parametrizável**
- **VHost:** `partner`
- **Exchange:** `partner.events`
- **Fila principal:** `partner.import.jobs`
- **Dead Letter Queue:** `partner.import.dlq`

### Configuração 100% parametrizada
- `Host`
- `Port`
- `VHost`
- `Username`
- `Password`
- `Exchange`
- `Queue Names`

### Observações
- **Não** utilizar nomes DEV/HML/PRD nas filas.
- **Separação de ambientes por instância RabbitMQ.**

---

## 1) Fluxo completo (end-to-end)

```
Upload
  ↓
ImportJob (persistência inicial)
  ↓
RabbitMQ (exchange → fila principal)
  ↓
Worker (consumer)
  ↓
Banco (linhas, erros, progresso)
  ↓
Status (job)
  ↓
Notificação do usuário
```

### Descrição detalhada
1. **Upload**
   - API recebe arquivo (CSV), valida cabeçalho e metadados.
   - Cria `ImportJob` com status `Queued`.
   - Armazena o arquivo em storage seguro (disk/Blob/S3).
   - Publica mensagem no RabbitMQ.

2. **ImportJob → RabbitMQ**
   - Mensagem inclui `JobId`, `PublicId`, `CompanyId`, `Feature`, `FilePath`, `CorrelationId`, `QueuedAtUtc`.
   - Producer publica na exchange `partner.events` com routing key `partner.import.job.queued`.

3. **Worker**
   - Consome fila `partner.import.jobs`.
   - Marca job como `Running` com `locked_by/locked_at`.
   - Processa em streaming e lotes (ex.: 100–500 linhas).
   - Atualiza contadores e erros.

4. **Banco / Status**
   - Atualiza `processed_rows`, `success_rows`, `error_rows`.
   - Ao final: `Completed`, `CompletedWithErrors`, `Failed` ou `Cancelled`.

5. **Notificação**
   - Notificação persistida em `ImportNotifications`.
   - UI faz polling para informar usuário em qualquer tela.

---

## 2) Modelo de dados (proposto)

### 2.1 `ImportJobs`
| Campo | Tipo | Observação |
|---|---|---|
| `id` | int (PK) | ID interno |
| `public_id` | guid | ID público exposto ao front |
| `feature` | nvarchar(60) | Ex.: `clients-csv` |
| `file_name` | nvarchar(260) | Nome original |
| `file_path` | nvarchar(500) | Caminho do storage |
| `file_hash_sha256` | char(64) | Idempotência |
| `company_id` | int | Escopo da importação |
| `status` | nvarchar(30) | Ver seção **Status** |
| `total_rows` | int | Total detectado |
| `processed_rows` | int | Linhas processadas |
| `success_rows` | int | Linhas OK |
| `error_rows` | int | Linhas com erro |
| `duration_ms` | int | Duração total |
| `started_at_utc` | datetime2 | Quando iniciou |
| `finished_at_utc` | datetime2 | Quando terminou |
| `created_by_user_id` | int | Usuário criador |
| `cancel_requested` | bit | Sinal de cancelamento |
| `cancel_requested_at_utc` | datetime2 | Timestamp do request |
| `cancelled_at_utc` | datetime2 | Timestamp efetivo |
| `attempts` | int | Tentativas de processamento |
| `last_error` | nvarchar(2000) | Erro final resumido |
| `locked_by` | nvarchar(100) | Worker atual |
| `locked_at_utc` | datetime2 | Quando lockado |
| `last_heartbeat_at_utc` | datetime2 | Health do worker |
| `correlation_id` | nvarchar(100) | Observabilidade |
| `retry_of_import_job_id` | int | Job anterior (retry) |

### 2.2 `ImportJobErrors`
| Campo | Tipo | Observação |
|---|---|---|
| `id` | int (PK) | |
| `import_job_id` | int (FK) | ImportJobs.id |
| `seq` | int | Sequência/linha |
| `line_number` | int | Linha CSV |
| `error_code` | nvarchar(50) | Código (opcional) |
| `message` | nvarchar(1000) | Mensagem de erro |
| `raw_line` | nvarchar(max) | Conteúdo da linha |
| `action` | nvarchar(30) | inserir/atualizar/excluir |
| `document` | nvarchar(30) | CPF/CNPJ |
| `email` | nvarchar(150) | |
| `created_at_utc` | datetime2 | |

### 2.3 `ImportNotifications`
| Campo | Tipo | Observação |
|---|---|---|
| `id` | int (PK) | |
| `import_job_id` | int (FK) | ImportJobs.id |
| `user_id` | int | Usuário destinatário |
| `title` | nvarchar(200) | Título |
| `message` | nvarchar(1000) | Mensagem |
| `status` | nvarchar(20) | `Unread`/`Read` |
| `created_at_utc` | datetime2 | |
| `read_at_utc` | datetime2 | |

### 2.4 `ImportJobItems` (avaliado para idempotência)
> **Decisão:** manter **tabela de itens** para idempotência fina por linha.

| Campo | Tipo | Observação |
|---|---|---|
| `id` | int (PK) | |
| `import_job_id` | int (FK) | ImportJobs.id |
| `seq` | int | Linha / posição |
| `line_hash` | char(64) | Hash da linha |
| `status` | nvarchar(30) | Processing/Processed/Failed |
| `processed_at_utc` | datetime2 | |
| `error_id` | int | ImportJobErrors.id (opcional) |
| `target_key` | nvarchar(100) | Identificador de destino |

---

## 3) Idempotência (estratégia definitiva)

### 3.1 Idempotência por Job
- **Chave:** `(company_id, feature, file_hash_sha256)` dentro de janela de tempo.
- Se existir job recente com status não-terminal, retornar o job existente.

### 3.2 Idempotência por Linha
- **ImportJobItems** com `seq` + `line_hash` único.
- Se linha já processada, ignorar e registrar log `ImportRowSkipped`.

### 3.3 Reprocessamento
- Retry deve **reutilizar o arquivo** e criar novo job com `retry_of_import_job_id`.
- Itens processados no job original **não** são reutilizados automaticamente (novo job = nova execução completa).

---

## 4) Status (máquina de estados)

### Estados
- `Queued`
- `Running`
- `Completed`
- `CompletedWithErrors`
- `Failed`
- `Cancelled`

### Transições permitidas
- `Queued → Running`
- `Queued → Cancelled`
- `Running → Completed | CompletedWithErrors | Failed | Cancelled`

### Cancelamento
- Usuário solicita **cancelamento** → job recebe `cancel_requested = true`.
- Worker verifica sinal a cada N linhas.
- Ao confirmar: status = `Cancelled`.

---

## 5) UX (telas)

### 5.1 Histórico de importações
- Lista com filtros (data, status, empresa, usuário).
- Campos: arquivo, empresa, status, total, processadas, sucesso, erro, duração.
- Ações: detalhes, baixar erros, cancelar, retry (quando aplicável).

### 5.2 Detalhes da importação
- Resumo: total, sucesso, erro, duração, status.
- Tabela de erros por linha.

### 5.3 Progresso
- Exibir `%` (processed/total) e status textual.
- Atualização via polling.

### 5.4 Download de erros
- Endpoint para exportar erros CSV/JSON.

---

## 6) Notificações (sem SignalR)

### Estratégia
- **Persistir notificações** em `ImportNotifications`.
- **Polling** no front-end a cada X segundos.
- Notificação exibida globalmente, independente da tela.

### Fluxo
1. Worker finaliza job
2. Cria notificação persistida
3. Front consulta `/api/import/notifications`

---

## 7) Observabilidade

### Campos obrigatórios
- `CorrelationId`
- `JobId`
- `Seq` (linha)

### Logs estruturados (exemplos)
- `ImportJobStarted`
- `ImportRowProcessed`
- `ImportRowFailed`
- `ImportCompleted`
- `ImportCancelled`

### Métricas
- Duração total por job
- Taxa de falha por job
- Linhas por segundo

---

## 8) Integração futura com VTEX (desacoplada)

### Princípio
- Importação e VTEX **não devem estar acopladas**.

### Plano futuro
- Criar **fila/worker** específico para VTEX.
- Cada linha válida gera evento `VtexSyncJob`.
- Feature flag `Vtex:Enabled` e subflags por módulo.

---

## 9) Entregáveis finais

✅ Documento de desenho arquitetural final (este arquivo).  
✅ Diretrizes de dados, idempotência, status, UX, notificações, observabilidade e VTEX.

***

**Próximo passo (fora deste escopo):** implementação faseada seguindo o roadmap aprovado.

***