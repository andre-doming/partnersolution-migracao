# Relatório FASE 1 — MFA (Persistência: Models + Queries)

Data: 2026-05-30

## Objetivo da fase
Implementar a fundação de persistência necessária para MFA (Models/Dtos + Queries Dapper) sem alterar endpoints, contratos HTTP ou frontend.

## Fontes oficiais seguidas
- `Documents/Login_and_Token/mfa_final_design.md`
- `Documents/Login_and_Token/sql_mfa_totp.sql`

## Arquivos criados
- `Migracao/Partner.Api/Features/Auth/Mfa/MfaModels.cs`
- `Migracao/Partner.Api/Features/Auth/Mfa/MfaQueries.cs`

## Arquivos alterados
- **Nenhum** (nesta fase)

## Decisões tomadas
1) Models/Dtos mapeados diretamente ao schema oficial (SQL Server) sem alterações de estrutura.
2) Queries Dapper parametrizadas para:
   - leitura do estado MFA (`tb_usuario_mfa`)
   - setup/enable/reset do MFA
   - lockouts (senha e MFA)
   - recovery codes (inserir/listar/invalidar/usar)
   - pending session (`tb_auth_mfa_pending`)
   - auditoria (`tb_audit_log`)
3) Mantida a decisão arquitetural: lockouts em `tb_usuario_mfa`.

## Riscos encontrados
- **Nenhum risco funcional** identificado nesta fase (sem alterações de runtime/endpoints).

## Build & testes
- `dotnet build Migracao\Partner.Api\Partner.Api.csproj` ✅
- `dotnet test Migracao\Partner.Api.Tests\Partner.Api.Tests.csproj` ✅ (9 tests OK)

## Observações para a FASE 2
- Implementar serviços de TOTP/Base32/criptografia seguindo o schema `MfaSecret VARBINARY(512)`.
- Garantir que os endpoints de setup usem `UpdateMfaSetupStarted` e `EnableMfa`.
- Manter logs sem dados sensíveis (secret/totp/recovery).

---

Status: **FASE 1 concluída, aguardando aprovação para avançar.**
