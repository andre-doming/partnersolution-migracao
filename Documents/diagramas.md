# Diagramas (Mermaid)

## Arquitetura geral
```mermaid
flowchart LR
    subgraph Frontend
        WEB[Partner.Web (Angular)]
    end

    subgraph Backend
        API[Partner.Api (.NET 8)]
        AUTH[Auth & MFA]
        USERS[Users]
        COMP[Companies]
        CLIENTS[Clients]
        IMPORT[Import CSV]
    end

    subgraph Infra
        DB[(SQL Server)]
        LOGS[Serilog Logs]
    end

    WEB --> API
    API --> AUTH
    API --> USERS
    API --> COMP
    API --> CLIENTS
    API --> IMPORT
    AUTH --> DB
    USERS --> DB
    COMP --> DB
    CLIENTS --> DB
    IMPORT --> DB
    API --> LOGS
```

## Login tradicional
```mermaid
sequenceDiagram
    participant U as Usuário
    participant W as Partner.Web
    participant A as Partner.Api
    participant DB as SQL Server

    U->>W: Login (usuário/senha)
    W->>A: POST /api/auth/login
    A->>DB: Validar usuário + senha
    DB-->>A: Dados válidos
    A-->>W: JWT + claims
    W-->>U: Sessão autenticada
```

## Login com MFA
```mermaid
sequenceDiagram
    participant U as Usuário
    participant W as Partner.Web
    participant A as Partner.Api
    participant DB as SQL Server

    U->>W: Login (usuário/senha)
    W->>A: POST /api/auth/login
    A->>DB: Validar usuário + senha
    DB-->>A: MFA habilitado
    A-->>W: pendingToken
    W-->>U: Solicita TOTP
    U->>W: Informa TOTP
    W->>A: POST /api/auth/mfa/verify
    A->>DB: Validar TOTP / recovery
    DB-->>A: OK
    A-->>W: JWT + claims
    W-->>U: Sessão autenticada
```

## Fluxo de importação CSV
```mermaid
sequenceDiagram
    participant U as Usuário
    participant W as Partner.Web
    participant A as Partner.Api
    participant DB as SQL Server

    U->>W: Upload CSV
    W->>A: POST /api/import/csv
    A->>A: Validar estrutura + permissões
    A->>DB: Processar linhas (insert/update/delete)
    DB-->>A: Resultado por linha
    A-->>W: Relatório de importação
    W-->>U: Exibe status e erros
```

## Fluxo de permissões
```mermaid
flowchart LR
    U[Usuário] -->|Login| JWT[JWT Claims]
    JWT -->|permissions, companies, admin| API[Partner.Api]
    API -->|Policies| ENDPOINTS[Endpoints protegidos]
    ENDPOINTS -->|Validações| DB[(SQL Server)]
```