# Plano de Implementação Incremental — MFA TOTP

Pasta: `Documents/Login_and_Token`

Fontes oficiais:
- `mfa_final_design.md`
- `sql_mfa_totp.sql`

Este documento atende ao pedido **antes de alterar arquivos**:

1) listar arquivos a criar
2) listar arquivos a alterar
3) listar dependências NuGet
4) listar dependências Angular
5) listar riscos de regressão

E define a execução por fases (FASE 1 a 8).

---

## 1) Arquivos que serão criados

### Backend (.NET) — `Migracao/Partner.Api`

Nova pasta (Vertical Slice dentro de Auth):
- `Migracao/Partner.Api/Features/Auth/Mfa/`

Arquivos novos planejados:
- `Migracao/Partner.Api/Features/Auth/Mfa/MfaModels.cs`
- `Migracao/Partner.Api/Features/Auth/Mfa/MfaQueries.cs`
- `Migracao/Partner.Api/Features/Auth/Mfa/Totp.cs` *(geração/validação RFC6238)*
- `Migracao/Partner.Api/Features/Auth/Mfa/Base32.cs` *(encode/decode sem dependência externa, se necessário)*
- `Migracao/Partner.Api/Features/Auth/Mfa/MfaSecretProtector.cs` *(encrypt/decrypt do secret; AES-GCM)*
- `Migracao/Partner.Api/Features/Auth/Mfa/RecoveryCodesService.cs`
- `Migracao/Partner.Api/Features/Auth/Mfa/MfaPendingTokenService.cs`
- `Migracao/Partner.Api/Features/Auth/Mfa/MfaAuditLogger.cs`

Opcional (se centralizar config):
- `Migracao/Partner.Api/Infrastructure/Security/MfaOptions.cs`

### Frontend (Angular) — `Migracao/Partner.Web`

Nova pasta:
- `Migracao/Partner.Web/src/app/features/auth/mfa/`

Arquivos novos planejados:
- `Migracao/Partner.Web/src/app/features/auth/mfa/mfa.models.ts`
- `Migracao/Partner.Web/src/app/features/auth/mfa/mfa.service.ts`
- `Migracao/Partner.Web/src/app/features/auth/mfa/mfa-challenge.component.ts`
- `Migracao/Partner.Web/src/app/features/auth/mfa/mfa-challenge.component.html`
- `Migracao/Partner.Web/src/app/features/auth/mfa/mfa-challenge.component.scss`
- `Migracao/Partner.Web/src/app/features/auth/mfa/mfa-setup.component.ts`
- `Migracao/Partner.Web/src/app/features/auth/mfa/mfa-setup.component.html`
- `Migracao/Partner.Web/src/app/features/auth/mfa/mfa-setup.component.scss`

---

## 2) Arquivos que serão alterados

### Backend — `Migracao/Partner.Api`
- `Migracao/Partner.Api/Features/Auth/AuthEndpoints.cs`
  - alterar `/login` para fluxo 2 etapas
  - mapear endpoints `/mfa/*`
- `Migracao/Partner.Api/Features/Auth/AuthModels.cs`
  - ajustar `AuthLoginResponse` (status + mfaToken + lockout info)
- `Migracao/Partner.Api/Features/Auth/AuthQueries.cs`
  - incluir queries de MFA (tb_usuario_mfa, recovery, pending)
- `Migracao/Partner.Api/Infrastructure/Security/AuthPolicies.cs` *(se necessário)*
  - policy/admin para reset/unlock
- `Migracao/Partner.Api/Features/Users/UserEndpoints.cs`
  - endpoints admin: reset MFA / unlock

### Frontend — `Migracao/Partner.Web`
- `Migracao/Partner.Web/src/app/core/auth/auth.service.ts`
  - adaptar contrato login para status/mfaToken
- `Migracao/Partner.Web/src/app/features/auth/login/login.component.ts`
- `Migracao/Partner.Web/src/app/features/auth/login/login.component.html`
- `Migracao/Partner.Web/src/app/app.routes.ts`
  - adicionar rotas `/mfa` e `/mfa/setup`
- `Migracao/Partner.Web/src/app/features/users/users.page.ts` *(admin actions)*
- `Migracao/Partner.Web/src/app/features/users/users.service.ts` *(calls reset/unlock)*

---

## 3) Dependências NuGet necessárias

O objetivo é **mínimo possível**.

### Obrigatórias (prováveis)
- `QRCoder` *(para gerar QR code PNG no backend; alternativa é gerar no frontend, mas o plano aprovado prevê gerar pela API)*

> Observação: TOTP e Base32 podem ser implementados com `System.Security.Cryptography` sem pacotes adicionais.

### Não adicionar
- MediatR
- ASP.NET Identity completo
- qualquer ORM além do Dapper existente

---

## 4) Dependências Angular necessárias

Idealmente **nenhuma nova dependência**.

Usaremos Angular Material já existente:
- inputs/buttons/cards/snackbar

Para QRCode na tela de setup:
- como o backend retorna `qrCodePngBase64`, o frontend só renderiza `<img [src]>` (sem libs de QR no Angular).

---

## 5) Riscos de regressão

1) **Quebra do contrato do `/api/auth/login`**
   - Mitigação: deploy coordenado backend+frontend; testes integrados.
2) **Fluxo de lockout afetando logins legítimos**
   - Mitigação: parâmetros configuráveis + auditoria + mensagens genéricas.
3) **Problemas com relógio/TOTP**
   - Mitigação: tolerância -1/+1 step e instruções na UI.
4) **Falhas de criptografia/chave mestra**
   - Mitigação: validação de config no startup e fallback seguro (bloquear setup/login com erro observável).
5) **Vazamento de segredos em logs**
   - Mitigação: revisar logs, nunca logar secret/totp/recovery.
6) **Dados órfãos na pending table**
   - Mitigação: expiração + cleanup por job simples.

---

# Execução por fases (implementação)

> Observação: as fases abaixo serão executadas em **ACT MODE**, com commits/checkpoints por fase.

## FASE 1
**Banco de dados** + **Models** + **Queries Dapper**

Entregas:
- aplicar/validar `sql_mfa_totp.sql`
- criar models (`tb_usuario_mfa`, `tb_usuario_mfa_recovery`, `tb_auth_mfa_pending`)
- criar queries Dapper em `MfaQueries.cs`

## FASE 2
**Secret TOTP** + **QRCode** + **Setup MFA**

Entregas:
- TOTP (RFC6238)
- encrypt/decrypt secret
- endpoint setup/start e setup/confirm

## FASE 3
**Login em duas etapas** + **MFA Pending Token**

Entregas:
- alterar `/auth/login` para retornar status + `mfaToken`
- implementar `tb_auth_mfa_pending` via service
- endpoint `/auth/mfa/verify` emitindo JWT definitivo

## FASE 4
**Recovery Codes**

Entregas:
- geração + hash + persistência
- verify por recovery code
- regeneração de lote

## FASE 5
**Lockout**

Entregas:
- regras de contagem/bloqueio na etapa 1 (senha)
- regras de contagem/bloqueio na etapa 2 (MFA)
- endpoint admin unlock

## FASE 6
**Auditoria**

Entregas:
- gravação de eventos em `tb_audit_log`
- correlation id consistente

## FASE 7
**Frontend Angular**

Entregas:
- telas `/mfa` e `/mfa/setup`
- ajuste do login e roteamento por status
- ações admin (reset/unlock)

## FASE 8
**Testes**

Entregas:
- unit tests do TOTP
- integration tests dos endpoints (se estrutura de testes permitir)
- validação manual guiada

---

## Ao final
Executar:
- build backend
- build frontend
- testes
E gerar relatório final da implementação.
