# MFA Final Design — TOTP obrigatório (RFC 6238)

Pasta: `Documents/Login_and_Token`

Status: **Arquitetura final aprovada para execução (sem implementação de código nesta etapa)**.

---

## 1) Visão geral da arquitetura MFA

### Objetivo
Implementar **autenticação em dois fatores obrigatória** para todos os usuários humanos, usando **TOTP (RFC 6238)** compatível com:

- Google Authenticator
- Microsoft Authenticator
- FortiToken Mobile
- Authy
- Bitwarden
- 1Password
- qualquer app compatível com RFC 6238

### Princípios de segurança
- **Nenhum JWT definitivo é emitido** antes do segundo fator, quando MFA estiver ativo/configurável.
- **Secret TOTP nunca é armazenado em claro** (armazenado criptografado).
- Recovery codes são armazenados **somente como hash** e **single-use**.
- Proteção contra brute force em **duas superfícies**:
  - senha (etapa 1)
  - MFA (etapa 2)
- Auditoria de eventos com **CorrelationId** para encadear ações.

### Decisão arquitetural (lockout state)
**Decisão adotada:** manter os controles de lockout (senha e MFA) em `tb_usuario_mfa`.

Campos nesta tabela:
- `FailedPasswordAttempts`, `PasswordLockoutUntil`
- `FailedMfaAttempts`, `MfaLockoutUntil`

Motivação resumida:
- menor complexidade e risco no curto prazo
- menos joins/pontos de falha no fluxo de autenticação
- consistência transacional mais simples

> Observação: no código, tratar a linha como “estado de segurança do usuário” (conceito), mesmo que o nome físico da tabela esteja em `tb_usuario_mfa`.

---

## 2) Referência de banco (fonte única da verdade)

**Fonte única do schema SQL**:
- `Documents/Login_and_Token/sql_mfa_totp.sql`

Este script cria/garante:

### `tb_usuario_mfa`
**Objetivo:** estado 1:1 de MFA + lockout por usuário.

Responsabilidades:
- habilitação/configuração MFA (`MfaEnabled`, `MfaConfiguredAt`, `MfaResetRequired`)
- secret TOTP criptografado (`MfaSecret`)
- timestamps úteis (`MfaSetupStartedAt`, `RecoveryCodesGeneratedAt`, `LastSuccessfulMfaAt`)
- controles de brute force:
  - senha: `FailedPasswordAttempts`, `PasswordLockoutUntil`
  - MFA: `FailedMfaAttempts`, `MfaLockoutUntil`

### `tb_usuario_mfa_recovery`
**Objetivo:** persistir recovery codes (apenas hash) por usuário.

Responsabilidades:
- single-use (`Used`, `UsedAt`)
- invalidação do lote (`Invalidated`, `InvalidatedAt`)

### `tb_auth_mfa_pending`
**Objetivo:** armazenar com segurança o **token temporário** da etapa 2 (login pendente de MFA).

Responsabilidades:
- token opaco com **hash** e expiração (`TokenHash`, `ExpiresAt`)
- consumo único (`ConsumedAt`)
- reforço anti-replay opcional (IP/UserAgent)

### `tb_audit_log`
**Objetivo:** trilha de auditoria de segurança/MFA.

Eventos mínimos (baseline):
- `MFA_ENABLED`
- `MFA_RESET`
- `MFA_FAILED`
- `MFA_LOCKED`
- `USER_UNLOCKED`
- `RECOVERY_CODE_USED`

> Observação: o script também adiciona colunas opcionais (ex.: `ActorUserId`, `IpAddress`, `UserAgent`, `MetadataJson`) se não existirem.

---

## 3) Fluxo Login → MFA → JWT (2 etapas)

### Etapa 1 — Login com senha
Endpoint: `POST /api/auth/login`

Responsabilidades:
1) Validar credenciais (sem vazar informação de usuário existente).
2) Aplicar lockout de senha:
   - incrementa `FailedPasswordAttempts` em falha
   - aplica `PasswordLockoutUntil` ao atingir limite configurado
   - zera contador em sucesso
3) Decidir próximo passo:
   - se MFA ativo/configurado: retorna `MFA_REQUIRED` + `mfaToken`
   - se MFA não configurado ou `MfaResetRequired=1`: retorna `MFA_SETUP_REQUIRED` + `mfaToken`
   - (caso excepcional) se MFA não for exigido: `LOGIN_SUCCESS` + JWT (não usado nesta versão, pois MFA é obrigatório)

Retorno **não deve conter JWT definitivo** quando o status for `MFA_REQUIRED` ou `MFA_SETUP_REQUIRED`.

### Etapa 2 — Verificação do segundo fator
Endpoint: `POST /api/auth/mfa/verify`

Responsabilidades:
1) Validar `mfaToken` (pendente):
   - expiração
   - consumo único
2) Validar fator:
   - TOTP (6 dígitos)
   - ou recovery code
3) Aplicar lockout de MFA:
   - incrementa `FailedMfaAttempts` em falha
   - aplica `MfaLockoutUntil` ao atingir limite
   - zera contador em sucesso
4) Em sucesso:
   - emitir JWT definitivo
   - registrar `LastSuccessfulMfaAt`
   - auditar eventos relevantes

---

## 4) Fluxo de Setup MFA (obrigatório)

### Setup Start
Endpoint: `POST /api/auth/mfa/setup/start`

Regras:
- requer `mfaToken` válido
- gera secret TOTP (20 bytes) e salva **criptografado** em `tb_usuario_mfa.MfaSecret`
- marca `MfaSetupStartedAt`
- retorna:
  - `otpauthUri`
  - QR code
  - secret em Base32 para fallback manual

### Setup Confirm
Endpoint: `POST /api/auth/mfa/setup/confirm`

Regras:
- valida TOTP com tolerância de clock (janela -1/+1 step)
- se válido:
  - `MfaEnabled=1`
  - `MfaConfiguredAt=now`
  - `MfaResetRequired=0`
  - gera 10 recovery codes e salva somente os hashes em `tb_usuario_mfa_recovery`
  - atualiza `RecoveryCodesGeneratedAt`
  - retorna os códigos **apenas uma vez**

---

## 5) Fluxo de Recovery Code

### Uso no login (etapa 2)
Endpoint: `POST /api/auth/mfa/verify` com `recoveryCode`.

Regras:
- comparar com hash (constant-time)
- marcar o code como `Used=1`, preencher `UsedAt`
- **invalidar todos os demais** recovery codes do usuário (mesmo lote)
- auditar `RECOVERY_CODE_USED`
- após autenticar com recovery code, usuário deve ser **obrigado a gerar novo conjunto**

### Regeneração do lote
Endpoint: `POST /api/auth/mfa/recovery/regenerate`

Regras:
- invalidar lote atual
- gerar novo conjunto de 10
- atualizar `RecoveryCodesGeneratedAt`
- retornar novos códigos **apenas uma vez**

---

## 6) Fluxo de Reset MFA (Admin)

Endpoint: `POST /api/users/{id}/mfa/reset`

Regras:
- apagar/zerar `MfaSecret`
- `MfaEnabled=0`
- `MfaResetRequired=1`
- invalidar recovery codes existentes
- auditar `MFA_RESET`

Observação de escopo:
- nesta versão, **não invalidar sessões JWT já emitidas** (expiram naturalmente).

---

## 7) Estratégia de Lockout

### Senha
- Em falha de senha: incrementa `FailedPasswordAttempts`.
- Ao atingir limite: define `PasswordLockoutUntil = now + N`.
- Em sucesso: zera `FailedPasswordAttempts`.

### MFA
- Em falha de MFA: incrementa `FailedMfaAttempts`.
- Ao atingir limite: define `MfaLockoutUntil = now + N`.
- Em sucesso: zera `FailedMfaAttempts`.

### Parametrização
Definir em configuração (appsettings), no mínimo:
- `PasswordMaxAttempts`
- `PasswordLockoutMinutes`
- `MfaMaxAttempts`
- `MfaLockoutMinutes`
- `MfaPendingTokenMinutes`

---

## 8) Estratégia de auditoria

Tabela: `tb_audit_log`.

Regras:
- Registrar eventos com `CorrelationId` consistente entre etapa 1 e etapa 2.
- Não registrar segredos (TOTP, secret, recovery code) em claro.
- Preferir `MetadataJson` para detalhes operacionais (quando habilitado no schema).

Eventos (baseline):
- `MFA_ENABLED`: setup confirmado
- `MFA_RESET`: reset por admin
- `MFA_FAILED`: tentativa inválida de MFA (ideal: registrar com parcimônia)
- `MFA_LOCKED`: lockout aplicado
- `USER_UNLOCKED`: desbloqueio por admin
- `RECOVERY_CODE_USED`: uso de recovery

---

## 9) Decisões arquiteturais adotadas

1) MFA obrigatório imediato para todos usuários humanos.
2) Login em **duas etapas**:
   - etapa 1 valida senha e retorna status + `mfaToken`
   - etapa 2 valida MFA e só então emite JWT
3) `mfaToken` temporário:
   - **opaco**, curto, consumo único
   - persistir **somente hash** em `tb_auth_mfa_pending`
4) Lockout de senha e MFA por usuário em `tb_usuario_mfa`.
5) Recovery codes:
   - 10 códigos no formato `XXXX-XXXX-XXXX`
   - single-use
   - ao usar 1, invalidar o restante e exigir novo lote
6) Não invalidar sessões já emitidas após reset MFA nesta versão.

---

## 10) Fora do escopo desta versão

- Desativar MFA pelo próprio usuário.
- “Lembrar dispositivo” / trust device.
- Outros fatores (SMS, e-mail OTP, push).
- WebAuthn/Passkeys.
- Exceções para contas técnicas (service accounts).
- Invalidar sessões ativas imediatamente após reset.
- Refresh tokens/blacklist/rotação.
- Rate limiting por IP (gateway) — recomendado como evolução.

---

## 11) Status final

Com este documento + o script `sql_mfa_totp.sql`, a arquitetura está **consolidada e pronta para implementação**.
