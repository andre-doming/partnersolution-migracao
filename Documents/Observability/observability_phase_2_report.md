# Fase 02 — Implementação Serilog JSON + Seq (Relatório)

## Resumo

Implementação de logs estruturados em JSON para a Partner.Api, integração opcional com Seq e atualização do runbook operacional conforme o plano aprovado na Fase 01.

## Arquivos alterados/criados

- `Migracao/Partner.Api/Program.cs`
  - Enriquecimento do contexto de request com propriedades padronizadas.
  - Habilitação opcional do sink do Seq via configuração (`Serilog:Seq`).
- `Migracao/Partner.Api/Middleware/CorrelationIdMiddleware.cs`
  - Padronização do nome da propriedade `CorrelationId` no `LogContext`.
- `Migracao/Partner.Api/appsettings.json`
  - Sink de arquivo em JSON (`CompactJsonFormatter`).
  - Enrichers (`WithMachineName`, `WithEnvironmentName`).
  - `Application` em `Serilog:Properties`.
  - Seção `Serilog:Seq` com `Enabled`, `ServerUrl`, `ApiKey`.
- `Migracao/Partner.Api/appsettings.Development.json`
  - `Serilog:Seq:Enabled = true` (mantém fallback para Console/File).
- `Migracao/Partner.Api/Partner.Api.csproj`
  - Pacotes adicionais para JSON e Seq.
- `docker/docker-compose.seq.yml`
  - Compose mínimo para Seq local.
- `docs/runbook.md`
  - Seção de observabilidade com uso do Seq e filtros.

## Pacotes adicionados

- `Serilog.Formatting.Compact` (3.0.0)
- `Serilog.Sinks.Seq` (8.0.0)

## Configurações criadas/ajustadas

### Serilog JSON (arquivo)

- Arquivo de log agora em `logs/partner-api-.json` com `CompactJsonFormatter`.
- Rolling diário e retenção mantidos.

### Propriedades padronizadas

- `Application` definido em `Serilog:Properties`.
- `Environment` e `MachineName` via enrichers (`WithEnvironmentName`, `WithMachineName`).
- `CorrelationId`, `UserId`, `Route`, `StatusCode`, `ElapsedMs` via `UseSerilogRequestLogging` e middlewares existentes.

### Seq (opcional)

```json
"Serilog": {
  "Seq": {
    "Enabled": false,
    "ServerUrl": "http://localhost:5341",
    "ApiKey": ""
  }
}
```

- `Enabled` pode ser ligado/desligado por ambiente.
- A aplicação sobe normalmente mesmo sem Seq disponível (fallback para Console/File).

## Como subir o Seq

```bash
docker compose -f docker/docker-compose.seq.yml up -d
```

### Porta utilizada

- UI/ingestão: `http://localhost:5341` (porta `5341` exposta para `80` no container).

## Como acessar o Seq

- Abrir `http://localhost:5341` no navegador.

## Evidências de validação

### Build

```bash
dotnet build Partner.Modern.sln
```

Resultado: **sucesso** (com 1 warning existente do projeto de testes).

### Testes

```bash
dotnet test Partner.Modern.sln
```

Resultado: **29 testes com sucesso**.

## Validações manuais (pendentes no ambiente local)

As validações abaixo exigem execução do serviço e do Seq localmente. Não foram executadas automaticamente nesta etapa:

- Startup da API.
- Escrita em arquivo JSON.
- Escrita no Seq.
- Consulta por `CorrelationId`.
- Consulta por `Error`.
- Consulta por `MFA`.
- Consulta por `RateLimit`.

## Limitações conhecidas

- Consultas de `MFA`, `Login` e `RateLimit` dependem de rotas específicas e do tráfego gerado no ambiente.
- A validação manual depende de Seq ativo e dados de execução reais.