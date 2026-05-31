# Runbook Operacional — Partner Solution

## Reset de MFA (Admin)
1. Identificar o usuário alvo (ID/LOGIN).
2. Executar endpoint/admin action para reset de MFA.
3. Confirmar que `MfaResetRequired = 1` e `MfaEnabled = 0`.
4. Usuário deverá refazer o setup no próximo login.

## Desbloqueio de usuário (senha)
1. Verificar `FailedPasswordAttempts` e `PasswordLockoutUntil`.
2. Resetar contadores para `0` e `NULL`.
3. Validar auditoria do evento de desbloqueio (quando implementado).

## Troubleshooting de login
- **401 Unauthorized sem mensagem**: senha incorreta ou usuário inativo.
- **401 com mensagem de lockout**: lockout de senha ativo.
- **MFA requerido**: usuário com MFA habilitado, precisa TOTP.
- **Setup obrigatório**: usuário sem MFA configurado.

## Troubleshooting de importação CSV
- Validar template e separador `;`.
- Confirmar permissões do usuário (`Import`).
- Verificar logs de importação por linha.
- Checar conexão com VTEX (se aplicável).

## Troubleshooting de health checks
- Conferir logs do Serilog em `Migracao/Partner.Api/logs`.
- Validar conexão com banco (ConnectionStrings__PartnerDb).
- Verificar variáveis de ambiente JWT e MFA.
