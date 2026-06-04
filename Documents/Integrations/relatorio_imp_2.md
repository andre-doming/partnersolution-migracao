# IMP-2 — Processamento real da importação assíncrona

## Resumo
Implementado o processamento real das importações assíncronas conforme `Documents/Integrations/import_async_final_design.md` e diretrizes do `relatorio_imp_1.md`. O worker agora executa validações, gravações de domínio, persistência de erros por linha, idempotência por item e atualização contínua de progresso, mantendo observabilidade estruturada.

---

## Escopo entregue
- Processamento real do CSV (ação inserir/atualizar/excluir).
- Validação de CPF por dígitos verificadores (sync + async).
- Persistência de erros por linha (`ImportJobErrors`).
- Controle de idempotência por item (`ImportJobItems`).
- Atualização de progresso a cada lote de linhas.
- Logs estruturados com `CorrelationId`, `JobId` e `JobPublicId`.

---

## Worker (ImportWorker)
Principais mudanças:
- Leitura do arquivo salvo (CSV ou seleção JSON).
- Validação de cabeçalho e regras de linha.
- Processamento com ações:
  - `inserir`: cria cliente quando inexistente.
  - `atualizar`: atualiza cliente existente.
  - `excluir`: inativa cliente existente.
- Grava erro por linha em `ImportJobErrors`.
- Idempotência por linha via `ImportJobItems` (hash SHA256 da linha).
- Atualização de progresso em batches (`ProgressBatchSize = 20`).
- Escopo de log com `CorrelationId`, `JobId`, `JobPublicId`.

---

## Endpoints (ImportEndpoints)
Alterações relevantes:
- Validação de CPF (11 dígitos + dígitos verificadores) nos fluxos sync/preview.
- Normalização de documento para 11 dígitos.
- Mascaramento de CPF nos logs de erro.

---

## Idempotência por item
- `ImportJobItems` recebe `ImportJobId`, `Seq` e `LineHash`.
- Em reprocessamentos, linhas já processadas são ignoradas.
- Linhas inválidas também são registradas como `Failed` com erro associado.

---

## Observabilidade
Eventos principais:
- `ImportJobStarted`
- `ImportRowProcessed`
- `ImportRowFailed`
- `ImportRowSkipped`
- `ImportCompleted`

Campos mínimos garantidos:
- `CorrelationId`
- `JobId`
- `JobPublicId`

---

## Arquivos alterados
- `Migracao/Partner.Api/Infrastructure/Import/ImportWorker.cs`
- `Migracao/Partner.Api/Features/Import/ImportEndpoints.cs`

---

## Limitações (mantidas)
- Sem tela Angular.
- Sem download de erros no frontend.
- Sem notificações frontend.
- Sem SignalR.
- Sem integração VTEX.

---

## Próximos passos sugeridos
- Expor endpoints de download de erros.
- Implementar notificações persistidas + polling.
- Finalizar UX de histórico e detalhe de importações.