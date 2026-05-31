# Relatório FASE 2 — MFA Core (TOTP + QRCode + Setup)

Data: 2026-05-30

## Objetivo da fase
Implementar a infraestrutura de MFA TOTP (RFC 6238) para cadastro e validação do fator, com geração de QR Code e recovery codes, **sem alterar o fluxo de login** ou contratos existentes.

## Fontes oficiais seguidas
- `Documents/Login_and_Token/mfa_final_design.md`
- `Documents/Login_and_Token/sql_mfa_totp.sql`

## Arquivos criados
- `Migracao/Partner.Api/Features/Auth/Mfa/MfaServices.cs`
- `Migracao/Partner.Api/Features/Auth/Mfa/MfaEndpoints.cs`
- `Migracao/Partner.Api.Tests/Auth/MfaTotpTests.cs`
- `Migracao/Partner.Api.Tests/Auth/MfaRecoveryCodeTests.cs`

## Arquivos alterados
- `Migracao/Partner.Api/Program.cs` (registro de `MapMfaEndpoints`)
- `Migracao/Partner.Api/Partner.Api.csproj` (adicionado QRCoder)

## Dependências adicionadas
- `QRCoder` 1.4.3 (geração de QRCode)

## Endpoints criados
- `POST /api/auth/mfa/setup`
  - Gera secret TOTP, Base32, URI otpauth e QRCode.
  - Registra `MFA_SETUP_STARTED`.
- `POST /api/auth/mfa/activate`
  - Valida TOTP, ativa MFA e gera recovery codes (retornados uma única vez).
  - Registra `MFA_ENABLED` e `MFA_RECOVERY_GENERATED`.

## Testes criados
- `MfaTotpTests` (geração/validação TOTP)
- `MfaRecoveryCodeTests` (hash e validação de recovery codes)

## Auditoria implementada
- `MFA_SETUP_STARTED`
- `MFA_ENABLED`
- `MFA_RECOVERY_GENERATED`
- `MFA_FAILED` (TOTP inválido na ativação)

## Build & testes
- `dotnet build Migracao\Partner.Api\Partner.Api.csproj` ✅
- `dotnet test Migracao\Partner.Api.Tests\Partner.Api.Tests.csproj` ✅ (13 tests OK)

## Riscos identificados
- **Criptografia do `MfaSecret`**: nesta fase o secret é persistido como `VARBINARY` sem criptografia adicional. Conforme o design oficial, será necessária camada de criptografia no armazenamento (FASE 3+).
- **Claims de login/usuário**: endpoints usam `userId` e `login` do token; garantir que JWT atual contém essas claims.

## Observações para a FASE 3
- Integrar o fluxo MFA ao login existente (`/api/auth/login`) sem alterar contratos atuais nesta fase.
- Implementar lockout de MFA e `mfaToken` pending (tb_auth_mfa_pending) conforme design.
- Adicionar validação de recovery code no fluxo `/api/auth/mfa/verify`.

---

Status: **FASE 2 concluída, aguardando aprovação para avançar.**
