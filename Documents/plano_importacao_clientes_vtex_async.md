# Importação de clientes — VTEX, compatibilidade de schema e evolução assíncrona

## 1) VTEX: estado atual e chave de desligamento

- No fluxo atual da API moderna de importação ([`ImportEndpoints.ImportClientsCsvAsync()`](Migracao/Partner.Api/Features/Import/ImportEndpoints.cs:52)), **não existe chamada para VTEX**.
- O processamento é local (banco SQL) com persistência em [`tb_cliente`](Migracao/Partner.Api/Features/Import/ImportQueries.cs:191), [`ImportJobs`](Migracao/Partner.Api/Features/Import/ImportQueries.cs:5) e [`ImportJobErrors`](Migracao/Partner.Api/Features/Import/ImportQueries.cs:25).
- Portanto, na implementação atual, a importação já está efetivamente “desligada” da VTEX.

### Recomendação

Mesmo sem integração ativa, manter um feature flag de governança para futuras versões:

- `Import:EnableVtexSync = false` (default)
- `Import:VtexSyncMode = "none" | "best-effort" | "strict"`

Assim, qualquer futura integração entra por configuração, sem risco de acoplamento implícito.

## 2) Fluxo legado (selecionar linhas antes de processar)

No legado, havia duas etapas: carregar CSV e selecionar registros para processar. Na versão atual ([`import.page.html`](Migracao/Partner.Web/src/app/features/import/import.page.html:7)), o fluxo é de etapa única (upload + processamento).

### Proposta de evolução (sem quebrar o endpoint atual)

1. **Etapa de pré-validação (preview)**
   - Novo endpoint: `POST /api/import/clients/preview`
   - Retorna linhas válidas/invalidas com identificador temporário (sessionId).

2. **Seleção de linhas no frontend**
   - Grid com checkbox por linha e ações “Selecionar válidas”, “Limpar seleção”.

3. **Processar selecionadas**
   - Novo endpoint: `POST /api/import/clients/process-selected`
   - Payload: `sessionId + selectedLineIds + companyId`.

4. **Compatibilidade retroativa**
   - Manter endpoint atual síncrono ([`/api/import/clients/csv`](Migracao/Partner.Api/Features/Import/ImportEndpoints.cs:52)) para operação rápida e scripts.

## 3) Campos `sexo` e `dt_nascimento`: banco local x VTEX

### Decisão para o ambiente atual

- Como o banco atual da migração está sem essas colunas (e sem `empresa/departamento/aprovado` em algumas instâncias), a importação deve funcionar em modo **schema-compatível**.
- Ajuste aplicado em [`ImportQueries.InsertClient`](Migracao/Partner.Api/Features/Import/ImportQueries.cs:191) e [`ImportQueries.UpdateClient`](Migracao/Partner.Api/Features/Import/ImportQueries.cs:226):
  - remoção de colunas ausentes do SQL de escrita
  - alinhamento com o padrão já usado em [`ClientQueries.Insert`](Migracao/Partner.Api/Features/Clients/ClientQueries.cs:115)

### Estratégia de produto

- Curto prazo: ignorar persistência desses campos no SQL quando não existirem.
- Médio prazo: adicionar colunas via migração controlada **ou** armazenar em tabela de extensão (`tb_cliente_ext`) para evitar quebra de ambientes heterogêneos.
- Longo prazo: sincronização opcional com VTEX via fila/evento, desacoplada da transação de import.

## 4) Evolução para assíncrono (>1000 linhas)

## Objetivo

Eliminar bloqueio de requisição HTTP longa e melhorar UX/observabilidade para arquivos grandes.

## Fase A — Enfileirar job

- Endpoint novo: `POST /api/import/clients/csv/async`
- A API:
  - valida arquivo/cabeçalho;
  - cria registro `ImportJobs` com status `Queued`;
  - persiste arquivo temporário seguro;
  - retorna `jobId` imediatamente.

## Fase B — Worker de processamento

- `BackgroundService` dedicado (ou Hangfire/Queue service).
- Consome jobs `Queued`, executa mesma regra de linha do método [`ProcessLineAsync()`](Migracao/Partner.Api/Features/Import/ImportEndpoints.cs:485).
- Atualiza `ImportJobs` com estados: `Running`, `Completed`, `CompletedWithErrors`, `Failed`.

## Fase C — Progresso em tempo real

- Endpoint de progresso: `GET /api/import/jobs/{id}/progress`
- Opcional: SignalR para push de progresso.
- Frontend de import ([`ImportPageComponent`](Migracao/Partner.Web/src/app/features/import/import.page.ts:38)) mostra barra de progresso e auto-refresh.

## Fase D — Resiliência e performance

- Batch size configurável (ex.: 200 linhas).
- Retry com backoff para falhas transitórias.
- Dead-letter para jobs inválidos.
- Idempotência por hash de arquivo + companyId + timestamp controlado.

## Observabilidade mínima

- Logs estruturados por `jobId`, `lineNumber`, `action`.
- Métricas: duração total, taxa linhas/s, % erro, top motivos de erro.
- Alertas para jobs `Failed` e tempo acima de SLA.

## Critérios de aceite

- Upload de 10k linhas retorna em < 2s com `jobId`.
- Usuário acompanha progresso sem refresh manual obrigatório.
- Resultado final mantém contadores e relatório de erros por linha.
- Sem degradação no endpoint síncrono legado da migração.

