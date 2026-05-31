# Plano de Rate Limiting — Partner Solution v2.0 (FASE 11)

**Origem:** limites aprovados pelo responsável do projeto.  
**Escopo:** apenas endpoints críticos de autenticação/MFA/admin.  
**Tecnologia:** ASP.NET Core Rate Limiter nativo (.NET 8).  
**Restrições:** sem Redis, sem alteração de banco, sem alteração de contratos HTTP.

---

## 1) Endpoints e Limites

### POST /api/auth/login
- **Partition:** IP + login normalizado
- **Limite:** 10 requisições por minuto
- **Resposta:** HTTP 429

### POST /api/auth/mfa/verify
- **Partition:** IP + PendingToken
- **Limite:** 20 requisições por minuto
- **Resposta:** HTTP 429

### POST /api/auth/mfa/activate
- **Partition:** usuário autenticado
- **Limite:** 10 requisições por minuto
- **Resposta:** HTTP 429

### POST /api/auth/mfa/setup
- **Partition:** usuário autenticado
- **Limite:** 5 requisições por minuto
- **Resposta:** HTTP 429

### POST /api/users/{id}/mfa/reset
- **Partition:** usuário autenticado (admin)
- **Limite:** 20 requisições por minuto
- **Resposta:** HTTP 429

### POST /api/users/{id}/mfa/unlock
- **Partition:** usuário autenticado (admin)
- **Limite:** 20 requisições por minuto
- **Resposta:** HTTP 429

---

## 2) Requisitos obrigatórios

- Utilizar exclusivamente ASP.NET Core Rate Limiter nativo (.NET 8).
- Não utilizar Redis ou infraestrutura externa.
- Não alterar banco.
- Não alterar contratos HTTP.
- Implementar `Retry-After` quando aplicável.
- Registrar logs estruturados contendo:
  - endpoint
  - correlationId
  - policy
  - rate limit hit

---

## 3) Observabilidade mínima

- Contadores por política (ex.: total de bloqueios por endpoint).
- Log estruturado em toda rejeição (429).
