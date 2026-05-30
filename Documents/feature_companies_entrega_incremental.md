# Entrega incremental — Feature Companies (Empresas)

## Escopo entregue

### Backend (.NET 8 + Minimal API + Dapper)

- Implementado slice de Companies com arquivos:
  - `Migracao/Partner.Api/Features/Companies/CompanyModels.cs`
  - `Migracao/Partner.Api/Features/Companies/CompanyQueries.cs`
  - `Migracao/Partner.Api/Features/Companies/CompanyValidators.cs`
  - `Migracao/Partner.Api/Features/Companies/CompanyEndpoints.cs`
- Endpoints implementados:
  - `GET /api/companies` (paginação SQL + filtros whitelist)
  - `GET /api/companies/{id}`
  - `POST /api/companies`
  - `PUT /api/companies/{id}`
  - `DELETE /api/companies/{id}` (inativação lógica)
- Regras de negócio:
  - normalização de CNPJ (somente dígitos)
  - validação de dígitos verificadores do CNPJ
  - validação de unicidade para empresa ativa
- Segurança/autorização:
  - policies por ação em Companies (`view/insert/update/delete`)
  - wiring concluído em `Shared/Extensions/ServiceCollectionExtensions.cs`

### Frontend (Angular standalone + Material + Reactive Forms)

- Implementado slice de Companies com arquivos:
  - `Migracao/Partner.Web/src/app/features/companies/companies.models.ts`
  - `Migracao/Partner.Web/src/app/features/companies/companies.service.ts`
  - `Migracao/Partner.Web/src/app/features/companies/companies.page.ts`
  - `Migracao/Partner.Web/src/app/features/companies/companies.page.html`
  - `Migracao/Partner.Web/src/app/features/companies/companies.page.scss`
  - `Migracao/Partner.Web/src/app/features/companies/company-form-dialog.component.ts`
  - `Migracao/Partner.Web/src/app/features/companies/company-form-dialog.component.html`
  - `Migracao/Partner.Web/src/app/features/companies/company-form-dialog.component.scss`
- Fluxos implementados:
  - listagem paginada
  - filtros whitelist alinhados ao backend
  - create/update via modal com Reactive Forms
  - inativação lógica
  - ações por permissão (`insert/update/delete`)
- Integração de navegação/permissões:
  - rota `/companies` adicionada
  - menu “Empresas” apontando para `/companies`
  - constantes de permissão Companies adicionadas em `auth-permissions.ts`

## Reuso aplicado da feature Users

- Mesmo padrão vertical slice (`Models + Queries + Validators + Endpoints` no backend; `models + service + page + dialog` no frontend).
- Mesmo padrão de paginação (`page`, `pageSize`, `total`, `items`).
- Mesmo padrão de filtro seguro por whitelist via `FilterSanitizer`.
- Mesmo padrão de policies por ação e checks por permissão no frontend.

## Validação executada

- Backend build:
  - `dotnet build Migracao/Partner.Api/Partner.Api.csproj` ✅
- Frontend build:
  - `npm run build` em `Migracao/Partner.Web` ✅

## Simplificações preservadas

- Delete lógico (sem exclusão física).
- Sem abstrações adicionais (sem MediatR/repositório genérico).
- SQL explícito com Dapper.
- CNPJ validado centralmente no backend.

## Pendências desta etapa

- Nenhuma pendência técnica bloqueante identificada para o escopo definido da feature Companies.

