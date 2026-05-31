# Relatório FASE 6 - Lockout de senha e ajustes de testes

## Objetivo
Consolidar a proteção contra brute force de senha no fluxo de login e validar o comportamento com testes de integração, mantendo a compatibilidade com o MFA já existente.

## Resumo das mudanças
### Backend (.NET)
- **AuthEndpoints**: adicionada mensagem de lockout de senha retornada como JSON (`message`) em respostas 401 quando o bloqueio está ativo ou foi aplicado.
- **AuthModels**: incluída a propriedade `Message` em `AuthLoginResponse` para transportar mensagens ao frontend.
- **Auditoria**: eventos de senha permanecem registrados (`PASSWORD_FAILURE`, `PASSWORD_LOCKOUT`, `PASSWORD_SUCCESS_AFTER_LOCKOUT`, `LOGIN_PASSWORD_OK`).

### Frontend (Angular)
- **mfa.models.ts**: adicionada propriedade `message` ao `LoginResponse`.
- **login.component.ts**: tratamento de erro atualizado para priorizar `error.message` vindo da API.

### Testes
- **PasswordLockoutIntegrationTests**: novos testes cobrindo incremento de tentativas, aplicação de lockout e reset após sucesso.
- **MfaIntegrationTests**: ajustes para tabelas em `dbo` (SQLite com `ATTACH DATABASE`) e consistência de consultas.
- **Execução**: `dotnet test Partner.Modern.sln` executado com sucesso (24 testes OK, 1 warning pré-existente em `UserAdminMfaTests.cs`).

## Arquivos alterados
### Backend
- `Migracao/Partner.Api/Features/Auth/AuthEndpoints.cs`
- `Migracao/Partner.Api/Features/Auth/AuthModels.cs`

### Frontend
- `Migracao/Partner.Web/src/app/features/auth/mfa/mfa.models.ts`
- `Migracao/Partner.Web/src/app/features/auth/login/login.component.ts`

### Testes
- `Migracao/Partner.Api.Tests/Auth/PasswordLockoutIntegrationTests.cs`
- `Migracao/Partner.Api.Tests/Auth/MfaIntegrationTests.cs`

## Comportamento esperado
- Tentativas inválidas de senha incrementam `FailedPasswordAttempts`.
- Ao atingir o limite configurado, `PasswordLockoutUntil` é definido e o login responde 401 com mensagem de bloqueio.
- Em login válido, contadores e lockout são zerados.
- Mensagem de lockout exibida na tela de login.

## Observações técnicas
- Foi necessário simular o schema `dbo` nos testes SQLite com `ATTACH DATABASE`.
- Queries `MfaQueries.EnsureMfaRowForUser` (T-SQL) foram substituídas nos testes por `INSERT OR IGNORE` compatível com SQLite.

## Próximos passos sugeridos
- Integrar o mesmo tratamento de mensagens para lockout de MFA (quando aplicável) no frontend.
- Considerar mensagens de erro internacionalizadas (i18n).
- Avaliar unificação das fixtures de teste (helper para SQLite + schema dbo).

## Não abordado nesta fase
- Alterações de UI específicas para lockout de MFA.
- Envio de notificações para o usuário ao bloquear.
- Integração com rate limit a nível de API Gateway.