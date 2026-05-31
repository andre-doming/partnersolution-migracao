# Relatório — FASE 11 (Rate Limiting)

**Data:** 31/05/2026  
**Objetivo:** implementar rate limiting nativo do .NET 8 para endpoints críticos, conforme plano aprovado.

---

## 1) Plano de referência

- Documento base: `Documents/Login_v2/rate_limiting_plan.md`

---

## 2) Políticas criadas (Rate Limiter)

| Policy | Endpoint | Partition | Limite |
|---|---|---|---|
| `auth-login` | `POST /api/auth/login` | IP + login normalizado | 10 req/min |
| `auth-mfa-verify` | `POST /api/auth/mfa/verify` | IP + PendingToken | 20 req/min |
| `auth-mfa-activate` | `POST /api/auth/mfa/activate` | usuário autenticado | 10 req/min |
| `auth-mfa-setup` | `POST /api/auth/mfa/setup` | usuário autenticado | 5 req/min |
| `admin-mfa-reset` | `POST /api/users/{id}/mfa/reset` | usuário autenticado (admin) | 20 req/min |
| `admin-mfa-unlock` | `POST /api/users/{id}/mfa/unlock` | usuário autenticado (admin) | 20 req/min |

---

## 3) Logs estruturados e observabilidade

**Logs em 429 (Rate limit hit):**
- endpoint
- correlationId
- policy
- rateLimitHit

**Métricas básicas:**
- contadores por política (rejeições)

---

## 4) Arquivos alterados/criados

### Backend
- `Migracao/Partner.Api/Program.cs`
- `Migracao/Partner.Api/Shared/Extensions/ServiceCollectionExtensions.cs`
- `Migracao/Partner.Api/Features/Auth/AuthEndpoints.cs`
- `Migracao/Partner.Api/Features/Auth/Mfa/MfaEndpoints.cs`
- `Migracao/Partner.Api/Features/Users/UserEndpoints.cs`
- `Migracao/Partner.Api/Infrastructure/RateLimiting/RateLimitingPolicyNames.cs`
- `Migracao/Partner.Api/Infrastructure/RateLimiting/RateLimitingMetrics.cs`
- `Migracao/Partner.Api/Infrastructure/RateLimiting/RateLimitingPartitionResolver.cs`
- `Migracao/Partner.Api/Infrastructure/RateLimiting/RateLimitingPolicyRegistry.cs`
- `Migracao/Partner.Api/Middleware/RateLimitingBodyCaptureMiddleware.cs`

### Testes
- `Migracao/Partner.Api.Tests/RateLimiting/RateLimitingPoliciesTests.cs`
- `Migracao/Partner.Api.Tests/RateLimiting/RateLimitingPartitionResolverTests.cs`
- `Migracao/Partner.Api.Tests/RateLimiting/RateLimitingBodyCaptureMiddlewareTests.cs`

### Documentação
- `Documents/Login_v2/rate_limiting_plan.md`
- `Documents/Login_v2/relatorio_fase_11_rate_limiting.md`

---

## 5) Validações executadas

```bash
dotnet build Partner.Modern.sln
dotnet test Partner.Modern.sln
```

**Resultado:**
- Build: OK (1 warning pré-existente em `UserAdminMfaTests.cs`)
- Tests: OK (29 testes)

---

## 6) Evidências de validação

- `dotnet build Partner.Modern.sln` → sucesso com 1 warning pré-existente (nulabilidade em `UserAdminMfaTests.cs`).
- `dotnet test Partner.Modern.sln` → 29 testes executados com sucesso.

---

## 7) Observações

- Sem alteração de banco, frontend, contratos HTTP, JWT ou MFA funcional.
- Rate Limiter nativo .NET 8, sem Redis ou infraestrutura externa.
