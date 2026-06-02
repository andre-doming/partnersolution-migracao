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

## Observabilidade — Serilog JSON + Seq

### Como subir o Seq
1. Executar: `docker compose -f docker/docker-compose.seq.yml up -d`.
2. Acessar a UI em: `http://localhost:5341`.

### Como acessar o Seq
- Abrir `http://localhost:5341` no navegador.
- Verificar se o serviço aparece como **running** no Docker.

### Como localizar um CorrelationId
- Filtrar no Seq por `CorrelationId = "<valor>"`.
- O valor é devolvido no header `X-Correlation-Id` das respostas da API.

### Como pesquisar erros
- Filtrar por `@Level = 'Error'`.
- Ou combinar com `StatusCode >= 500`.

### Como localizar requests lentas
- Filtrar por `ElapsedMs > 1000`.
- Ordenar por `ElapsedMs` desc.

### Como analisar Rate Limit
- Filtrar por rota de rate limiting (ex.: `Route` contendo `ratelimit`).
- Combinar com `StatusCode = 429` para bloqueios.

### Como analisar MFA
- Filtrar por `Route` contendo `mfa`.
- Verificar `StatusCode` e `UserId` para correlação.

### Como analisar Login
- Filtrar por `Route` contendo `login`.
- Analisar `StatusCode` e `CorrelationId` para troubleshooting.
