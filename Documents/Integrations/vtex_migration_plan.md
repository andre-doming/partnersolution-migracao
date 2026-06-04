# Planejamento — Migração da integração VTEX (Legado → API moderna)

> **Status:** planejamento de migração (sem implementação).  
> **Objetivo:** deixar “engatilhado” um caminho seguro e observável para migrar a integração VTEX do legado para a API moderna.

## 1) Contexto e estado atual

### Legado (SolutionsTools.*)

Integrações VTEX estão no projeto legado **SolutionsTools.Services** com credenciais hardcoded:

- `SolutionsTools.Services/ClientServices.cs`
  - Base URL: `https://api.vtex.com/lojabestoff/dataentities/`
  - Entity: `CL`
  - Headers: `x-vtex-api-appkey`, `x-vtex-api-apptoken`
- `SolutionsTools.Services/PartnerServices.cs`
  - Base URL: `https://api.vtex.com/lojabestoff/dataentities/`
  - Entity: `PR`
  - Headers: `x-vtex-api-appkey`, `x-vtex-api-apptoken`
- `SolutionsTools.Services/OrderServices.cs`
  - Base URL: `https://lojabestoff.vtexcommercestable.com.br/api/oms/pvt/`
  - Endpoints: `/orders/{orderNo}`, `/orders?q={email}`
  - Headers: `x-vtex-api-appkey`, `x-vtex-api-apptoken`

### Moderna (Migracao/Partner.Api)

- **Não há integração VTEX ativa** na API moderna.
- O fluxo de importação é **100% SQL**, sem chamada VTEX (ver `Documents/plano_importacao_clientes_vtex_async.md`).

## 2) Objetivos da migração

1. Migrar integração VTEX para a API moderna com **segurança** e **governança**.
2. Habilitar integração **assíncrona** (sem bloquear request).
3. Garantir **observabilidade** (logs/Seq por operação VTEX).
4. Permitir **liga/desliga** via feature flag (por módulo e por ambiente).

## 3) Escopo recomendado

### 3.1 Master Data (CL/PR)
- Leitura por ID/CPF/email e listagens.
- CRUD (Create/Update/Delete) usado por fluxos de clientes/empresas.

### 3.2 OMS (Orders)
- Consulta de pedidos por número e por email.

## 4) Arquitetura proposta

### 4.1 Camadas

- `Infrastructure/Integrations/Vtex/`
  - `IVtexClient`
  - `VtexClient` (HttpClient)
  - DTOs (`MasterData`, `Oms`)
  - Mapeadores

### 4.2 Configuração (sem credenciais hardcoded)

```json
"Vtex": {
  "Enabled": false,
  "MasterData": {
    "Enabled": false,
    "BaseUrl": "https://api.vtex.com/lojabestoff/dataentities",
    "AppKey": "__REQUIRED_FROM_ENV__",
    "AppToken": "__REQUIRED_FROM_ENV__"
  },
  "Oms": {
    "Enabled": false,
    "BaseUrl": "https://lojabestoff.vtexcommercestable.com.br/api/oms/pvt/",
    "AppKey": "__REQUIRED_FROM_ENV__",
    "AppToken": "__REQUIRED_FROM_ENV__"
  }
}
```

### 4.3 Feature flags (liga/desliga)

- `Vtex:Enabled` (global)
- `Vtex:MasterData:Enabled`
- `Vtex:Oms:Enabled`

Com isso é possível:

- desligar tudo em produção rapidamente;
- habilitar somente MasterData;
- testar OMS isoladamente.

## 5) Execução assíncrona e persistência

### 5.1 Persistência de jobs

Criar tabela `VtexSyncJobs` (ex.: SQL):
- `JobId` (guid)
- `EntityType` (CL/PR/ORDER)
- `Operation` (UPSERT/DELETE/READ)
- `PayloadRef` (FK ou JSON)
- `Status` (Queued/Running/Completed/Failed)
- `Attempts`
- `LastError`
- `CreatedAt/UpdatedAt`

### 5.2 Worker

`BackgroundService` para processar jobs:
- retry com backoff
- idempotência
- dead-letter para falhas persistentes

## 6) Observabilidade

Logs estruturados com campos:

- `ExternalSystem = "VTEX"`
- `ExternalOperation` (ex.: `MasterData.CL.Upsert`)
- `ExternalUrl`
- `StatusCode`
- `ElapsedMs`
- `CorrelationId`

**Seq**: criar queries/painéis simples por `ExternalSystem=VTEX`.

## 7) Segurança

- AppKey/AppToken **via env/secret store**.
- Segredo nunca em código.
- Permitir rotação sem redeploy.

## 8) Fases sugeridas

### FASE VTEX-01 — Client MasterData (leitura)
- Implementar client somente leitura (GET/search)
- Feature flag `Vtex:MasterData:Enabled`
- Observabilidade básica

### FASE VTEX-02 — Client MasterData (upsert)
- Upsert via job assíncrono
- Persistência de jobs + worker

### FASE VTEX-03 — OMS (Orders)
- Consultas por email/orderNo
- Feature flag `Vtex:Oms:Enabled`

## 9) Critérios de aceite

- Nenhuma credencial hardcoded na moderna.
- Feature flags funcionais (on/off global e por módulo).
- Jobs assíncronos com retry e status persistido.
- Logs estruturados por operação VTEX no Seq.

## 10) Observações finais

- A migração pode coexistir com o legado por um tempo.
- Recomendado criar uma **camada de compatibilidade** para comparar respostas (legado x novo) em ambiente de homologação.