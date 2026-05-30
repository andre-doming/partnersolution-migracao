# Entrega incremental — Feature Importação CSV (síncrona)

## Escopo entregue

### Planejamento e análise

- Documento de planejamento publicado em `Documents/plano_feature_import_csv.md` com:
  - plano incremental
  - riscos técnicos
  - preocupações de performance
  - estratégia de observabilidade
  - estratégia de evolução futura sem reescrita

### Backend (.NET 8 + Minimal API + Dapper)

- Implementado slice de Importação CSV em:
  - `Migracao/Partner.Api/Features/Import/ImportModels.cs`
  - `Migracao/Partner.Api/Features/Import/ImportQueries.cs`
  - `Migracao/Partner.Api/Features/Import/ImportEndpoints.cs`

- Endpoints implementados:
  - `POST /api/import/clients-csv`
    - upload CSV síncrono (multipart)
    - validação de arquivo, cabeçalho e linhas
    - normalização e processamento incremental linha a linha
    - resumo detalhado e retorno de erros por linha
  - `GET /api/import/lookups`
    - empresas permitidas para o usuário
  - `GET /api/import/jobs`
    - histórico simples com paginação
  - `GET /api/import/jobs/{id}`
    - detalhe do job + erros

- Persistência operacional (controle de importação):
  - criação idempotente de tabelas:
    - `ImportJobs`
    - `ImportJobErrors`
  - status e contadores por execução
  - duração em ms e horários UTC

- Regras implementadas (V1 pragmática):
  - colunas esperadas do modelo legado:
    - `nome;sobrenome;cpf;email;sexo;dt_nascimento;departamento;cargo;acao`
  - validações por linha:
    - ação (`inserir`, `atualizar`, `excluir`)
    - documento (11/14 dígitos)
    - e-mail válido (quando informado)
    - data de nascimento (`dd/MM/yyyy` ou `yyyy-MM-dd`)
  - escopo por empresa + autorização do usuário
  - SQL explícito e parametrizado com Dapper

- Observabilidade básica:
  - logs estruturados por job
  - logs de erro por linha
  - medição de tempo total via `Stopwatch`

### Frontend (Angular + Material + Reactive Forms)

- Implementado slice de import em:
  - `Migracao/Partner.Web/src/app/features/import/import.models.ts`
  - `Migracao/Partner.Web/src/app/features/import/import.service.ts`
  - `Migracao/Partner.Web/src/app/features/import/import.page.ts`
  - `Migracao/Partner.Web/src/app/features/import/import.page.html`
  - `Migracao/Partner.Web/src/app/features/import/import.page.scss`

- Funcionalidades entregues:
  - upload simples CSV + seleção de empresa
  - feedback visual de processamento (`progress bar`)
  - resumo detalhado da execução
  - exibição de erros por linha
  - histórico simples de importações com paginação

- Navegação atualizada:
  - rota `/import`
  - menu "Importar" apontando para `/import`

## Decisões de simplificação mantidas

- processamento síncrono (sem fila/event bus/worker)
- sem abstrações genéricas de framework de importação
- foco em clareza e manutenção

## Validação executada

- `dotnet build Migracao/Partner.Api/Partner.Api.csproj` ✅
- `npm run build` em `Migracao/Partner.Web` ✅

