# Plano final de execução — MFA obrigatório (TOTP)

Pasta: `Documents/Login_and_Token`

Este documento complementa o `plano_mfa_totp.md` com:

1. Scripts SQL completos (referência)
2. Plano incremental de implementação
3. Lista exata de arquivos a alterar (backend + frontend)
4. Estratégia de rollback
5. Checklist de validação

---

## 1) Scripts SQL completos

Arquivo fonte:
- `Documents/Login_and_Token/sql_mfa_totp.sql`

Conteúdo do script:
- `tb_usuario_mfa` (1:1)
  - `MfaSecret VARBINARY(512)`
  - `MfaSetupStartedAt`
  - `RecoveryCodesGeneratedAt`
  - contadores/lockouts
- `tb_usuario_mfa_recovery`
- `tb_auth_mfa_pending` (armazenamento seguro do token temporário da etapa 2)
- `tb_audit_log` (cria se não existir e adiciona colunas opcionais)
- backfill: cria registro em `tb_usuario_mfa` para todos os usuários existentes

Recomendação operacional:
- Rodar o script primeiro em **DEV**, depois **HML**, por último **PROD**.
- Registrar o hash/versão do script executado (ex.: em ferramenta de migração usada no projeto).

---

## 2) Plano incremental de implementação (sem código ainda)

### Fase 0 — Preparação
1) Confirmar estratégia de criptografia do `MfaSecret` (AES-GCM recomendado) e onde ficará a chave mestra.
2) Definir seção de configuração:
   - `Security:Mfa:PasswordMaxAttempts`
   - `Security:Mfa:PasswordLockoutMinutes`
   - `Security:Mfa:MfaMaxAttempts`
   - `Security:Mfa:MfaLockoutMinutes`
   - `Security:Mfa:MfaPendingTokenMinutes`
3) Definir convenções de auditoria (EventType + descrição + metadata mínima).

### Fase 1 — Banco
1) Aplicar `sql_mfa_totp.sql`.
2) Validar:
   - constraints FK
   - índices criados
   - backfill executado

### Fase 2 — Backend: infraestrutura de MFA
Criar camada mínima (Shared/Infrastructure) para:
1) Gerar e validar TOTP (RFC6238):
   - secret (20 bytes)
   - Base32 encode/decode
   - janela de tolerância (-1/+1 step)
2) Criptografia em repouso:
   - `IMfaSecretProtector` (encrypt/decrypt)
3) Recovery codes:
   - geração (10 códigos `XXXX-XXXX-XXXX`)
   - hashing seguro (PBKDF2/Identity hasher)
4) `mfaToken` temporário:
   - geração token opaco
   - persistência `TokenHash` + expiração + consumo único
   - validação constant-time
5) Auditoria:
   - helper `IAuditLogger` (grava `tb_audit_log`)

### Fase 3 — Backend: endpoints (vertical slices)
1) Alterar `POST /api/auth/login` para fluxo 2 etapas.
2) Implementar:
   - `POST /api/auth/mfa/verify`
   - `POST /api/auth/mfa/setup/start`
   - `POST /api/auth/mfa/setup/confirm`
   - `POST /api/auth/mfa/recovery/regenerate`
3) Implementar endpoints admin:
   - `POST /api/users/{id}/mfa/reset`
   - `POST /api/users/{id}/unlock`
4) Implementar regras de lockout:
   - senha (incrementa no login)
   - MFA (incrementa no verify)

### Fase 4 — Frontend Angular
1) Ajustar tela de login para lidar com `status` e `mfaToken`.
2) Criar rotas/páginas:
   - `/mfa` desafio
   - `/mfa/setup` wizard
3) Adicionar no módulo de usuários (admin):
   - ação “Resetar MFA”
   - ação “Desbloquear”
4) Garantir tratamento de erros genéricos (não vazar detalhes).

### Fase 5 — Testes e hardening
1) Testes unitários do validador TOTP.
2) Testes de integração dos endpoints (Dapper + SQL):
   - happy path
   - lockout
   - recovery codes
3) Testes e2e manuais (scripts).

---

## 3) Lista exata de arquivos que serão alterados

> Observação: a lista abaixo está “exata” no sentido de **alvos planejados** dado o estado atual do repositório. Quando entrarmos em ACT MODE para implementar, eu sigo esta lista como baseline.

### 3.1 Backend — `Migracao/Partner.Api`

**Arquivos existentes a alterar**:
- `Migracao/Partner.Api/Features/Auth/AuthEndpoints.cs`
  - mudar `/login` para 2 etapas
  - adicionar mapeamentos `/mfa/*`
- `Migracao/Partner.Api/Features/Auth/AuthModels.cs`
  - ajustar `AuthLoginResponse` para incluir `status`, `mfaToken`, lockout info
- `Migracao/Partner.Api/Features/Auth/AuthQueries.cs`
  - novas queries para `tb_usuario_mfa`, `tb_usuario_mfa_recovery`, `tb_auth_mfa_pending`

**Arquivos novos a criar (planejado)**:
- `Migracao/Partner.Api/Features/Auth/Mfa/` *(nova pasta de slice)*
  - `MfaModels.cs`
  - `MfaQueries.cs`
  - `TotpService.cs` (ou `Totp.cs`)
  - `MfaSecretProtector.cs`
  - `RecoveryCodesService.cs`
  - `MfaPendingTokenService.cs`
  - `MfaAudit.cs` (helper)

**Users/Admin**:
- `Migracao/Partner.Api/Features/Users/UserEndpoints.cs`
  - incluir endpoints admin: reset MFA, unlock
- `Migracao/Partner.Api/Infrastructure/Security/AuthPolicies.cs`
  - se necessário criar policy específica para reset/unlock (ou usar admin)

### 3.2 Frontend — `Migracao/Partner.Web`

**Arquivos existentes a alterar**:
- `Migracao/Partner.Web/src/app/core/auth/auth.service.ts`
  - ajustar contrato de `login()` para lidar com `status`/`mfaToken`
- `Migracao/Partner.Web/src/app/features/auth/login/login.component.ts`
- `Migracao/Partner.Web/src/app/features/auth/login/login.component.html`
- `Migracao/Partner.Web/src/app/app.routes.ts`
  - adicionar rotas `/mfa` e `/mfa/setup`

**Arquivos novos a criar (planejado)**:
- `Migracao/Partner.Web/src/app/features/auth/mfa/` 
  - `mfa-challenge.component.ts|html|scss`
  - `mfa-setup.component.ts|html|scss`
  - `mfa.models.ts`
  - `mfa.service.ts`

**Users/Admin UI** (existente):
- `Migracao/Partner.Web/src/app/features/users/users.page.ts`
  - adicionar ações no grid
- `Migracao/Partner.Web/src/app/features/users/users.service.ts`
  - incluir chamadas reset/unlock

### 3.3 Documentos
- `Documents/Login_and_Token/plano_mfa_totp.md` *(já atualizado)*
- `Documents/Login_and_Token/sql_mfa_totp.sql` *(criado)*
- `Documents/Login_and_Token/plano_execucao_mfa_totp.md` *(este arquivo)*

---

## 4) Estratégia de rollback

### 4.1 Rollback de banco
Como criamos novas tabelas (baixo acoplamento), rollback é controlado:
1) Se precisar reverter antes de deploy do backend:
   - `DROP TABLE tb_auth_mfa_pending;`
   - `DROP TABLE tb_usuario_mfa_recovery;`
   - `DROP TABLE tb_usuario_mfa;`
   - manter `tb_audit_log` (não recomendado remover; é histórico)

2) Se backend já estiver em produção:
   - preferir rollback por **feature toggle** (ver 4.3) ou hotfix,
   - não apagar tabelas com dados de auditoria/recovery.

### 4.2 Rollback de aplicação
- Reverter versão do backend e frontend.
- Como o login muda de contrato, o rollback deve considerar:
  - publicar backend+frontend em conjunto,
  - ou manter compatibilidade temporária (fora de escopo; preferir deploy coordenado).

### 4.3 Mitigação recomendada (toggle)
Mesmo com MFA obrigatório, é prudente incluir um toggle emergencial (somente config):
- `Security:Mfa:EnforceMfa = true` (default true)
Em incidente:
- setar false para permitir login sem exigir MFA enquanto corrige.

> Este toggle não muda a decisão de “obrigatório”; é uma trava operacional.

---

## 5) Checklist de validação

### 5.1 Banco
- [ ] Tabelas criadas: `tb_usuario_mfa`, `tb_usuario_mfa_recovery`, `tb_auth_mfa_pending`
- [ ] Backfill executado (todos usuários têm linha em `tb_usuario_mfa`)
- [ ] Índices criados
- [ ] `tb_audit_log` possui colunas adicionais (se adotado)

### 5.2 Fluxo de login
- [ ] Usuário com MFA ativo: `/login` retorna `MFA_REQUIRED` e **não** emite JWT
- [ ] Usuário sem MFA: `/login` retorna `MFA_SETUP_REQUIRED` e bloqueia acesso
- [ ] `/mfa/verify` emite JWT apenas após TOTP válido

### 5.3 Setup MFA
- [ ] QR code/secret gerado e escaneável por Google/Microsoft Authenticator
- [ ] Confirmação aceita com tolerância de clock (janela -1/+1)
- [ ] Recovery codes exibidos 1 vez
- [ ] `RecoveryCodesGeneratedAt` preenchido

### 5.4 Recovery codes
- [ ] Um recovery code válido autentica
- [ ] Ao usar 1 code, os demais são invalidados
- [ ] Sistema obriga regeneração do lote

### 5.5 Brute force e lockout
- [ ] 5 erros de senha bloqueiam login por 15 min (config)
- [ ] 5 erros de MFA bloqueiam verify por 15 min (config)
- [ ] Admin consegue desbloquear

### 5.6 Auditoria/observabilidade
- [ ] Eventos gravados: `MFA_ENABLED`, `MFA_RESET`, `MFA_FAILED`, `MFA_LOCKED`, `RECOVERY_CODE_USED`, `USER_UNLOCKED`
- [ ] `CorrelationId` presente e consistente entre etapas
- [ ] Sem logs com TOTP/secret/recovery em claro

### 5.7 Segurança geral
- [ ] Sem enumeração de usuários por mensagens
- [ ] `mfaToken` é opaco, curto, uso único e armazenado apenas como hash
- [ ] Secret armazenado criptografado (`VARBINARY(512)`)
