# Fase 01 — Observabilidade Básica com Serilog + Seq (Plano)

> **Status:** Planejamento apenas (sem implementação nesta fase).

## 1. Objetivo da fase

Evoluir a observabilidade do Partner Solution mantendo simplicidade operacional, preparando logs estruturados com Serilog e integração futura com Seq.

### Fora de escopo (nesta fase)

- Prometheus
- Grafana
- OpenSearch
- OpenTelemetry

## 2. Estado atual identificado

Referências principais:

- `Migracao/Partner.Api/Program.cs`
- `Migracao/Partner.Api/appsettings.json`

Pontos já existentes:

- `UseSerilog` com leitura de configuração via `appsettings`.
- `UseSerilogRequestLogging` com enriquecimento do contexto de request.
- `CorrelationIdMiddleware` e `RequestMetricsMiddleware` ativos.
- Logs em arquivo (`logs/partner-api-.log`) via `Serilog.Sinks.File`.

Lacunas para esta fase:

- Log em arquivo ainda é texto simples (não estruturado em JSON).
- Não há `Serilog.Sinks.Seq` configurado.
- Propriedades padrão solicitadas ainda não estão uniformizadas com o casing final.
- `Application`, `Environment` e `MachineName` não estão definidos como propriedades padrão.

## 3. Diretriz de logs estruturados (JSON)

Planejar a conversão do sink de arquivo para formato **JSON estruturado**:

- Manter rolling diário e retenção simples (como já configurado).
- Padronizar nomes das propriedades com PascalCase.
- Manter console ativo como fallback local.

## 4. Propriedades padrão (mínimo obrigatório)

Campos a serem adicionados como padrão:

| Propriedade | Origem planejada | Observação |
|---|---|---|
| `Application` | Valor fixo (`Partner.Api`) | Identifica serviço |
| `Environment` | `ASPNETCORE_ENVIRONMENT` | Ex.: Development/Production |
| `MachineName` | Enrichment do host | Nome do nó |
| `CorrelationId` | `CorrelationIdMiddleware` | Já existe middleware |
| `UserId` | Claim `PartnerClaimTypes.UserId` | Se autenticado |
| `Route` | Endpoint/Path | `DisplayName` ou `Request.Path` |
| `StatusCode` | `HttpContext.Response.StatusCode` | Código de resposta |
| `ElapsedMs` | `RequestMetricsMiddleware` | Tempo de request |

## 5. Preparação para integração com Seq

Planejar futura integração com Seq:

- Adicionar `Serilog.Sinks.Seq`.
- Configurar URL via `appsettings`/variável de ambiente.
- Garantir que a aplicação não dependa do Seq para iniciar.
- Manter Console + File(JSON) como fallback local.

## 6. Docker Compose mínimo para Seq local

Planejar um compose simples dedicado ao Seq local (sem implementação nesta fase):

```yaml
services:
  seq:
    image: datalust/seq:latest
    container_name: partner-seq
    environment:
      - ACCEPT_EULA=Y
    ports:
      - "5341:80"
    volumes:
      - seq-data:/data

volumes:
  seq-data:
```

Observações:

- Arquivo sugerido: `docker/docker-compose.seq.yml`.
- A UI ficará em `http://localhost:5341`.

## 7. Runbook operacional (planejado)

### 7.1 Como subir o Seq

1. Executar `docker compose -f docker/docker-compose.seq.yml up -d`.
2. Acessar `http://localhost:5341`.

### 7.2 Como consultar logs

- Usar a interface do Seq e filtrar por `Application` e `Environment`.

### 7.3 Como localizar um CorrelationId

- Filtrar por `CorrelationId = "<valor>"` no Seq.
- Opcional: usar o mesmo valor para localizar requisições relacionadas.

### 7.4 Como filtrar erros

- Filtrar por `@Level = 'Error'`.
- Adicionalmente filtrar por `StatusCode >= 500`.

### 7.5 Como identificar requisições lentas

- Filtrar por `ElapsedMs` (ex.: `ElapsedMs > 1000`).
- Ordenar por `ElapsedMs` desc.

## 8. Critérios de aceite da fase

- Documento criado em `Documents/Observability/observability_phase_1_plan.md`.
- O plano cobre Serilog, logs JSON, Seq local e runbook operacional.
- O documento explicita ausência de implementação nesta fase.