# Entrega incremental — Feature Users

## Escopo entregue

Entrega ponta a ponta da feature **Users** com backend + frontend, respeitando Vertical Slice enxuto.

## Backend

- Endpoints implementados em `Features/Users/UserEndpoints.cs`:
  - `GET /api/users` (paginação + filtros whitelist)
  - `GET /api/users/lookups`
  - `GET /api/users/{id}`
  - `POST /api/users`
  - `PUT /api/users/{id}`
  - `DELETE /api/users/{id}` (inativação lógica)
- DTOs e modelos em `Features/Users/UserModels.cs`.
- SQL explícito Dapper em `Features/Users/UserQueries.cs`.
- Validações de request em `Features/Users/UserValidators.cs`.
- Regras de autorização por policy:
  - `UsersPolicy`, `UsersInsertPolicy`, `UsersUpdatePolicy`, `UsersDeletePolicy`.
- Validações de negócio:
  - login/e-mail ativo únicos
  - empresas e funções ativas válidas
  - necessidade de ao menos uma empresa e uma permissão
- Vínculos usuário-empresa e usuário-permissão persistidos em transação.

## Frontend

- Feature criada em `src/app/features/users`:
  - `users.page.ts/html/scss`
  - `users.service.ts`
  - `users.models.ts`
  - `user-form-dialog.component.ts/html/scss`
- Integração de rota em `src/app/app.routes.ts` (`/users`).
- Controle de permissão por ação:
  - visualizar, criar, editar, inativar
- Tabela Material com paginação e filtros.
- Modal com Reactive Forms para create/update.

## Ajustes de auth/authz relacionados

- Novas permissões frontend em `src/app/core/auth/auth-permissions.ts`:
  - `funcUsuariosIns`, `funcUsuariosUpd`, `funcUsuariosDel`.
- Item de menu “Usuários” direcionado para `/users` em `layout/shell.component.ts`.

## Validação técnica

- Backend build: sucesso (`dotnet build Partner.Modern.sln -c Debug`).
- Frontend build: sucesso (`npm run build` em `Migracao/Partner.Web`).

## Pendências da próxima iteração

1. Melhorar UX de confirmação de inativação (substituir `confirm` por dialog Material).
2. Melhorar feedback de erros de validação (mapear mensagens campo a campo no formulário).
3. Reavaliar orçamento inicial de bundle Angular após entrada da nova tela.

