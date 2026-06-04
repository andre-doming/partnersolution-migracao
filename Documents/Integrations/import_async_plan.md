# Planejamento — Importação assíncrona de planilhas (alto volume)

> **Status:** planejamento (sem implementação).  
> **Objetivo:** tornar importações de planilhas grandes escaláveis, resilientes e observáveis, com feedback claro ao usuário.

## 1) Problema atual

- Importações grandes (milhares de linhas) bloqueiam a request HTTP.
- Erros por linha são reportados, mas o processo é síncrono e demorado.
- Há necessidade de:
  - **assíncrono**
  - **fila/worker**
  - **status por job**
  - **visibilidade ao usuário**

## 2) Padrão de mercado (recomendado)

### 2.1 Fluxo “Upload + Job + Worker”

1. **Upload do arquivo**
   - Endpoint recebe arquivo e valida cabeçalho.
   - Cria **job** com status `Queued`.
   - Armazena arquivo em local seguro (disco/Blob/S3).
   - Responde rápido com `jobId`.

2. **Worker processa em background**
   - Lê o arquivo por lotes.
   - Valida linha a linha.
   - Persiste no banco e envia para VTEX (se habilitado).
   - Atualiza status e métricas do job.

3. **Usuário acompanha status**
   - Tela de “Importações” com lista e filtros.
   - Status: `Queued`, `Running`, `Completed`, `CompletedWithErrors`, `Failed`.

### 2.2 Retorno ao usuário

- **Imediato:** devolve `jobId` + status inicial.
- **Acompanhamento:** endpoints de progresso e histórico.
- **Relatório final:** arquivo com erros por linha (CSV/JSON) + resumo.

## 3) Arquitetura proposta

### 3.1 Banco de dados

Tabela `ImportJobs` (exemplo):

- `JobId` (guid)
- `FileName`
- `Status` (`Queued`, `Running`, `Completed`, `CompletedWithErrors`, `Failed`)
- `TotalLines`
- `ProcessedLines`
- `SuccessCount`
- `FailureCount`
- `StartedAt`, `CompletedAt`
- `CreatedByUserId`
- `CompanyId`
- `ErrorsFilePath`

Tabela `ImportJobErrors`:

- `JobId`
- `LineNumber`
- `ErrorCode`
- `ErrorMessage`
- `Payload`

### 3.2 Worker

Implementação recomendada:

- `BackgroundService` (para início rápido)
- Escalável para:
  - Hangfire
  - RabbitMQ/Kafka + consumers
  - Azure Service Bus / SQS

### 3.3 Processamento em lote

- Ler CSV em **streaming** (não carregar inteiro em memória).
- Processar em **batches** (ex.: 100–500 linhas).
- Retry por lote com backoff.

## 4) Integração com VTEX

- **Desacoplada** do processamento principal.
- Após persistência local, enfileirar job “VTEX Sync”.
- Feature flag controla se a sincronização ocorre ou não.

## 5) Observabilidade

- Log por job/linha com:
  - `JobId`
  - `LineNumber`
  - `Status`
  - `ElapsedMs`
  - `ExternalSystem = VTEX` (quando aplicável)

- Métricas:
  - `ImportJobDurationMs`
  - `ImportJobFailureRate`
  - `LinesPerSecond`

## 6) UX / Tela de acompanhamento

### 6.1 Lista de importações

- Data/hora
- Usuário responsável
- Status
- Quantidade total/processada
- Ações: ver detalhes, baixar erros

### 6.2 Detalhe do job

- Resumo de sucesso/falhas
- Tabela com linhas inválidas

## 7) Fases sugeridas

### Fase A — Job + status básico
- Endpoint de upload cria job.
- Worker simples em background.
- Lista de jobs (somente status).

### Fase B — Erros por linha + download
- Persistir `ImportJobErrors`.
- Gerar arquivo de erros.

### Fase C — Integração assíncrona com VTEX
- Jobs separados para sync VTEX.
- Flags para ligar/desligar VTEX.

## 8) Critérios de aceite

- Upload responde em < 2s com `jobId`.
- Jobs processam em background sem bloquear request.
- Usuário consegue acompanhar status em UI.
- Relatório de erros por linha disponível.

---

Se você concordar, posso transformar esse plano em um **roadmap faseado com entregas e stories** e depois partimos para implementação gradual.