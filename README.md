# Partner Solution — Modernização (.NET 8 + Angular)

## Visão geral
Este repositório consolida a modernização incremental do **Partner Solution**, migrando o legado WebForms para uma arquitetura moderna baseada em **.NET 8 + Angular**, mantendo compatibilidade com o banco existente e evoluindo segurança, observabilidade e experiência do desenvolvedor.

O projeto é organizado em duas frentes principais:
- **Migracao/Partner.Api**: API .NET 8 (Vertical Slice, Dapper, JWT, MFA TOTP).
- **Migracao/Partner.Web**: Frontend Angular (features por domínio, guards e Material).

Também há pastas do legado (SolutionsTools.*) mantidas como referência histórica.

## Stack utilizada
- **Backend**: .NET 8, ASP.NET Core, Dapper, FluentValidation, Serilog
- **Frontend**: Angular 18, Angular Material, RxJS
- **Auth**: JWT + MFA TOTP (RFC 6238)
- **Observabilidade**: logs estruturados + CorrelationId
- **Banco**: SQL Server (compatibilidade com schema legado)

## Arquitetura
- **Vertical Slice** por feature (Auth, Users, Companies, Clients, Import)
- **Queries Dapper explícitas**, sem repositórios genéricos
- **Middleware global** para erros e correlação
- **Políticas de autorização** centralizadas no backend

Diagramas estão em **Documents/diagramas.md**.

## Principais decisões
- Dapper como acesso a dados por clareza e controle de SQL
- Vertical Slice “enxuto”, sem camadas excessivas
- JWT curto sem refresh token nesta fase
- MFA TOTP obrigatório para todos os usuários
- Hash de senha moderno com fallback legado

ADRs detalhados em **Documents/adr/**.

## Estrutura de pastas
```
.
├── Migracao/
│   ├── Partner.Api/          # API .NET 8
│   └── Partner.Web/          # Angular
├── Documents/                # Planos, relatórios e documentação
├── SolutionsTools.*          # Legado (referência)
└── docs/                     # Runbooks operacionais
```

## Setup local
### Backend
```bash
dotnet restore Partner.Modern.sln
dotnet build Partner.Modern.sln
dotnet test Partner.Modern.sln
```

### Frontend
```bash
cd Migracao/Partner.Web
npm install
npm run build
```

## Variáveis de ambiente (env vars)
Definir por variáveis de ambiente (ex.: `appsettings.json` usa `__REQUIRED_FROM_ENV__`).

**Backend**
- `ConnectionStrings__PartnerDb`
- `Jwt__SecretKey`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__ExpirationMinutes`
- `Mfa__MfaPendingTokenMinutes`
- `Mfa__MfaMaxAttempts`
- `Mfa__MfaLockoutMinutes`
- `PasswordLockout__PasswordMaxAttempts`
- `PasswordLockout__PasswordLockoutMinutes`

## Autenticação (JWT)
- Login tradicional retorna JWT com claims de usuário, admin, permissões e empresas.
- Token curto, sem refresh token nesta fase.
- Políticas de autorização aplicadas no backend.

## MFA (TOTP)
- MFA obrigatório para todos os usuários.
- Suporte RFC 6238 (Google/Microsoft/Authy/1Password/etc.).
- Setup obrigatório no primeiro login (QR Code + TOTP).
- Recovery codes suportados.
- Lockout temporário para brute force de MFA.

## Importação CSV
- Upload e processamento por linha.
- Validação de estrutura e permissões no backend.
- Logs de importação preservam status por linha.

## Observabilidade
- Logs estruturados com **Serilog**.
- **CorrelationId** para rastreio ponta a ponta.
- Logs locais em `Migracao/Partner.Api/logs` e `Migracao/Partner.Web`.

## Testes
- Testes de integração e unidade na solução `Partner.Modern.sln`.
- MFA e lockout cobertos por testes específicos em `Partner.Api.Tests`.

## Docker (placeholder)
Não há Dockerfiles oficiais nesta fase. Foram criados **placeholders mínimos** em `docker/` para referência futura.

## CI/CD (placeholder)
Não há pipeline oficial nesta fase. Foi criado um **placeholder** em `.github/workflows/ci-placeholder.yml` apenas como referência.

## Roadmap
- Evoluir pipeline CI/CD real (build/test backend + build frontend)
- Finalizar Dockerfiles e docker-compose oficiais
- Melhorar DX com scripts de bootstrap local
- Evoluir observabilidade com métricas e tracing

---
**Documento operacional**: `docs/runbook.md`  
**Relatório final**: `Documents/projeto_final_consolidado.md`