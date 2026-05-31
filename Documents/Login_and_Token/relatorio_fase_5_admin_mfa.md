# Relatório FASE 5 — Administração MFA

Data: 2026-05-30

## Objetivo
Implementar gerenciamento administrativo de MFA (reset e desbloqueio) e expor status MFA no módulo de usuários, sem alterar o fluxo de login, JWT, MFA Verify/Setup e recovery codes.

## Fontes oficiais
- `Documents/Login_and_Token/mfa_final_design.md`
- `Documents/Login_and_Token/sql_mfa_totp.sql`

## Endpoints criados
- `POST /api/users/{id}/mfa/reset`
  - Requer permissão **UsersMfaAdmin**.
  - Proíbe resetar o próprio usuário.
  - Reseta MFA, invalida recovery codes e remove sessões pendentes.
- `POST /api/users/{id}/mfa/unlock`
  - Requer permissão **UsersMfaAdmin**.
  - Proíbe desbloquear o próprio usuário.
  - Zera contadores e lockouts (senha + MFA).

## Permissões adicionadas
- `AuthPermissions.UsersMfaAdmin` (`funcUsuariosMfaAdmin`)
- `AuthPolicies.UsersMfaAdmin`
- Atualização correspondente em `AUTH_PERMISSIONS` no frontend.

## Exposição do estado MFA (admin)
Informações adicionadas no list de usuários (somente admin MFA):
- `MfaEnabled`
- `MfaConfiguredAt`
- `LastSuccessfulMfaAt`
- `MfaResetRequired`
- `FailedPasswordAttempts`
- `PasswordLockoutUntil`
- `FailedMfaAttempts`
- `MfaLockoutUntil`

## Auditoria
Eventos registrados:
- `ADMIN_MFA_RESET`
- `ADMIN_USER_UNLOCK`
Campos: `UserId`, `ActorUserId`, `CorrelationId`, `IpAddress`, `UserAgent`.

## Frontend (Users)
### Colunas adicionadas
- MFA
- MFA Configurado Em
- Último MFA
- Bloqueado

### Ações adicionadas
- Reset MFA
- Desbloquear usuário

### UX
- Confirmação de ação antes de executar.
- Mensagens claras de sucesso/erro.

## Arquivos criados
- `Migracao/Partner.Api.Tests/Users/UserAdminMfaTests.cs`
- `Migracao/Partner.Web/src/app/features/users/users.page.spec.ts`

## Arquivos alterados
### Backend
- `Migracao/Partner.Api/Features/Auth/AuthPermissions.cs`
- `Migracao/Partner.Api/Infrastructure/Security/AuthPolicies.cs`
- `Migracao/Partner.Api/Shared/Extensions/ServiceCollectionExtensions.cs`
- `Migracao/Partner.Api/Features/Auth/Mfa/MfaQueries.cs`
- `Migracao/Partner.Api/Features/Users/UserQueries.cs`
- `Migracao/Partner.Api/Features/Users/UserModels.cs`
- `Migracao/Partner.Api/Features/Users/UserEndpoints.cs`

### Frontend
- `Migracao/Partner.Web/src/app/core/auth/auth-permissions.ts`
- `Migracao/Partner.Web/src/app/features/users/users.models.ts`
- `Migracao/Partner.Web/src/app/features/users/users.service.ts`
- `Migracao/Partner.Web/src/app/features/users/users.page.ts`
- `Migracao/Partner.Web/src/app/features/users/users.page.html`
- `Migracao/Partner.Web/src/app/features/users/users.page.scss`

## Testes adicionados
- Backend: `UserAdminMfaTests` (valida bloqueio de reset no próprio usuário)
- Frontend: `users.page.spec.ts` (sanidade do componente)

## Validação executada
- `dotnet build Partner.Modern.sln` ✅ (warning: nulabilidade no teste fake)
- `dotnet test Partner.Modern.sln` ✅ (1 warning, 20 tests)
- `npm test -- --watch=false` ✅ (4 tests)
- `npm run build` ✅ (warning de budget: +5.42 kB)

## Riscos identificados
- **Budget Angular**: build excedeu em 5.42 kB (ajuste de budget pode ser necessário).
- **Warning de nulabilidade** no teste fake (`UserAdminMfaTests`) — não impacta runtime.

---

Status: **FASE 5 concluída** — aguardando aprovação.