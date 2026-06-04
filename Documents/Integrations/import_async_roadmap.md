# Roadmap — Importação assíncrona (fases + stories)

> **Status:** planejamento detalhado (sem código).  
> **Objetivo:** fornecer fases, entregas e histórias para implantação gradual da importação assíncrona.

## Visão geral

### Abordagem recomendada
1. **Começar simples**: job + worker interno.
2. **Evoluir resiliência**: retries, métricas e relatórios.
3. **Escalar**: migrar para fila real (mensageria).

### Princípios
- Não bloquear request HTTP.
- Não perder linhas válidas por falhas parciais.
- Feedback claro ao usuário (status + erros por linha).
- Integração VTEX desacoplada e controlada por feature flag.

---

## Fase 1 — MVP assíncrono (Job + Worker interno)

### Objetivo
Transformar o fluxo atual em **upload + job** com processamento em background.

### Entregas
- Endpoint de upload cria `ImportJob`.
- Worker interno processa linhas em lote.
- Status visível (Queued / Running / Completed / Failed).
- Relatório de erros por linha.

### Stories sugeridas

**US1 — Criar Job de Importação**
- Como usuário, quero subir uma planilha e receber um `jobId` para acompanhar o progresso.

**US2 — Processar Job em Background**
- Como sistema, quero processar as linhas em lote sem bloquear request.

**US3 — Persistir erros por linha**
- Como usuário, quero saber quais linhas falharam e por quê.

**US4 — Tela de acompanhamento**
- Como usuário, quero ver minhas importações com status e detalhes.

### Critérios de aceite
- Upload responde em < 2s com `jobId`.
- Worker processa por batches (ex.: 200 linhas).
- Job concluído com contadores e erros detalhados.

---

## Fase 2 — Resiliência e observabilidade

### Objetivo
Adicionar robustez operacional e métricas.

### Entregas
- Retry com backoff por lote.
- Logs estruturados com `JobId` e `LineNumber`.
- Métricas básicas (tempo total, linhas/s).

### Stories sugeridas

**US5 — Retry e tolerância a falhas**
- Como sistema, quero reprocessar lotes falhos automaticamente.

**US6 — Métricas do job**
- Como operação, quero acompanhar duração e taxa de falhas.

---

## Fase 3 — Integração assíncrona com VTEX

### Objetivo
Sincronizar VTEX sem impactar o fluxo principal.

### Entregas
- Job secundário “VTEX Sync”.
- Feature flag `Vtex:Enabled` + granular por módulo.
- Fail-safe: se VTEX falha, não derruba o job principal.

### Stories sugeridas

**US7 — Enfileirar sync VTEX**
- Como sistema, quero disparar sync VTEX só para linhas válidas.

**US8 — Desligar VTEX por configuração**
- Como operação, quero ligar/desligar a integração sem deploy.

---

## Fase 4 — Escala com mensageria (opcional)

### Objetivo
Escalar processamento horizontalmente.

### Entregas
- API somente cria job + publica evento.
- Worker externo consome fila.

### Story sugerida

**US9 — Migrar worker para mensageria**
- Como operação, quero escalar múltiplos workers sem alterar contrato da API.

---

## Próximo passo recomendado

Iniciar pela **Fase 1**, que já elimina o bloqueio de request e melhora UX rapidamente.

Se quiser, posso detalhar:
- estimativas por story,
- requisitos técnicos (tabelas/índices),
- e critérios de rollout por ambiente.