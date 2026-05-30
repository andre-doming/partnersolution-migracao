# Entrega incremental — Feature Clients (Clientes)

## Escopo entregue

### Backend (.NET 8 + Minimal API + Dapper)

- Implementado slice completo de Clients:
  - `Migracao/Partner.Api/Features/Clients/ClientModels.cs`
  - `Migracao/Partner.Api/Features/Clients/ClientQueries.cs`
  - `Migracao/Partner.Api/Features/Clients/ClientValidators.cs`
  - `Migracao/Partner.Api/Features/Clients/ClientEndpoints.cs`
- Endpoints implementados:
  - `GET /api/clients` (paginação SQL + filtros whitelist + filtro por empresa do usuário)
  - `GET /api/clients/lookups` (empresas permitidas para o usuário)
  - `GET /api/clients/{id}` (respeitando escopo de empresa)
  - `POST /api/clients`
  - `PUT /api/clients/{id}`
  - `DELETE /api/clients/{id}` (inativação lógica)
- Regras de negócio:
  - normalização de documento (somente dígitos)
  - normalização de e-mail (`trim + lower`)
  - unicidade de documento/e-mail por empresa ativa
  - validação de empresa ativa e acesso da empresa pelo usuário não-admin

### Auth/AuthZ

- Policies por ação de Clients adicionadas:
  - `ClientsInsert`, `ClientsUpdate`, `ClientsDelete`
- Wiring em autorização concluído em:
  - `Migracao/Partner.Api/Infrastructure/Security/AuthPolicies.cs`
  - `Migracao/Partner.Api/Shared/Extensions/ServiceCollectionExtensions.cs`

### Frontend (Angular standalone + Material + Reactive Forms)

- Implementado slice completo de Clients:
  - `Migracao/Partner.Web/src/app/features/clients/clients.models.ts`
  - `Migracao/Partner.Web/src/app/features/clients/clients.service.ts`
  - `Migracao/Partner.Web/src/app/features/clients/clients.page.ts`
  - `Migracao/Partner.Web/src/app/features/clients/clients.page.html`
  - `Migracao/Partner.Web/src/app/features/clients/clients.page.scss`
  - `Migracao/Partner.Web/src/app/features/clients/client-form-dialog.component.ts`
  - `Migracao/Partner.Web/src/app/features/clients/client-form-dialog.component.html`
  - `Migracao/Partner.Web/src/app/features/clients/client-form-dialog.component.scss`
- Fluxos implementados:
  - listagem com paginação
  - filtros reais com whitelist alinhada ao backend
  - filtro por empresa
  - modal reativo para create/update
  - inativação lógica
  - ações por permissão

### Navegação e permissões no frontend

- Atualizado `auth-permissions.ts` com ações de Clients (`insert/update/delete`).
- Atualizada rota `/clients` em `app.routes.ts`.
- Atualizado menu para navegação de Clientes em `shell.component.ts`.

## Preparação para futura importação CSV

- Contrato de dados de Clients já contempla campos essenciais (`document`, `email`, `companyId`, `department`, `role`, `approved`, `isActive`).
- Regras críticas concentradas no backend (normalização e validações), favorecendo reaproveitamento futuro em endpoint de importação.
- Sem acoplamentos extras a componentes genéricos ou camadas artificiais.

## Validação executada

- `dotnet build Migracao/Partner.Api/Partner.Api.csproj` ✅
- `npm run build` em `Migracao/Partner.Web` ✅

