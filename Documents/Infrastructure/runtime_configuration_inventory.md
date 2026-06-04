# 📋 Runtime Configuration Inventory — Partner.Modern

**Data:** 2026-06-04  
**Versão:** 1.0  
**Status:** PRÉ-IMP-8 — Configuração Local

---

## 📌 Sumário Executivo

Este documento mapeia **TODA** a configuração necessária para executar o Partner.Modern localmente. As configurações são carregadas via:

- ✅ `appsettings.json` (padrões globais)
- ✅ `appsettings.Development.json` (overrides local)
- ✅ **Variáveis de Ambiente** (override final - OBRIGATÓRIO)
- ✅ `IConfiguration` e `IOptions<T>`

---

## 🗄️ 1. BANCO DE DADOS

### ConnectionStrings:PartnerDb

| Propriedade | Valor |
|---|---|
| **Nome** | `ConnectionStrings:PartnerDb` |
| **Tipo** | `string` |
| **Obrigatório** | ⚠️ **SIM** |
| **Valor Padrão** | `__REQUIRED_FROM_ENV__` (placeholder) |
| **Arquivo Origem** | `appsettings.json` (linha 4) |
| **Finalidade** | String de conexão SQL Server |
| **Validação** | Mínimo 32 caracteres, não pode conter `__REQUIRED_FROM_ENV__` ou `CHANGE_ME` |
| **Variável Env** | `ConnectionStrings__PartnerDb` |
| **Exemplo** | `Server=localhost;UID=usr_partner;PWD=Pass@2026!!!;Database=db_partner;` |

**Validação em:** `ServiceCollectionExtensions.cs:320` (`EnsureConnectionStringConfigurationIsSafe`)

---

## 🔐 2. AUTENTICAÇÃO JWT

### Jwt:Issuer

| Propriedade | Valor |
|---|---|
| **Nome** | `Jwt:Issuer` |
| **Tipo** | `string` |
| **Obrigatório** | ✅ SIM |
| **Valor Padrão** | `"Partner.Api"` |
| **Arquivo Origem** | `appsettings.json` (linha 7) |
| **Variável Env** | `Jwt__Issuer` |
| **Finalidade** | Identificador do emissor do token JWT |
| **Validação** | Não pode estar vazio |

### Jwt:Audience

| Propriedade | Valor |
|---|---|
| **Nome** | `Jwt:Audience` |
| **Tipo** | `string` |
| **Obrigatório** | ✅ SIM |
| **Valor Padrão** | `"Partner.Web"` |
| **Arquivo Origem** | `appsettings.json` (linha 8) |
| **Variável Env** | `Jwt__Audience` |
| **Finalidade** | Identificador da audiência do token JWT |
| **Validação** | Não pode estar vazio |

### Jwt:SecretKey

| Propriedade | Valor |
|---|---|
| **Nome** | `Jwt:SecretKey` |
| **Tipo** | `string` |
| **Obrigatório** | ⚠️ **SIM** |
| **Valor Padrão** | `"CHANGE_ME_MIN_32_CHARS_FOR_DEV_ONLY_12345"` |
| **Arquivo Origem** | `appsettings.json` (linha 9) |
| **Variável Env** | `Jwt__SecretKey` |
| **Finalidade** | Chave secreta para assinar tokens JWT |
| **Validação** | Mínimo 32 caracteres, não pode conter `__REQUIRED_FROM_ENV__` ou `CHANGE_ME` |
| **Exemplo** | `"0123456789abcdef0123456789abcdef012345"` (44 caracteres) |

### Jwt:ExpirationMinutes

| Propriedade | Valor |
|---|---|
| **Nome** | `Jwt:ExpirationMinutes` |
| **Tipo** | `int` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `60` |
| **Arquivo Origem** | `appsettings.json` (linha 10) |
| **Variável Env** | `Jwt__ExpirationMinutes` |
| **Finalidade** | Tempo de expiração do token em minutos |
| **Validação** | Deve ser > 0 |

**Validação em:** `ServiceCollectionExtensions.cs:291` (`EnsureJwtConfigurationIsSafe`)

---

## 🔑 3. AUTENTICAÇÃO MFA (Multi-Factor Authentication)

### Mfa:MfaPendingTokenMinutes

| Propriedade | Valor |
|---|---|
| **Nome** | `Mfa:MfaPendingTokenMinutes` |
| **Tipo** | `int` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `10` |
| **Arquivo Origem** | `appsettings.json` (linha 13) |
| **Variável Env** | `Mfa__MfaPendingTokenMinutes` |
| **Finalidade** | Tempo de validade do token MFA pendente em minutos |

### Mfa:MfaMaxAttempts

| Propriedade | Valor |
|---|---|
| **Nome** | `Mfa:MfaMaxAttempts` |
| **Tipo** | `int` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `5` |
| **Arquivo Origem** | `appsettings.json` (linha 14) |
| **Variável Env** | `Mfa__MfaMaxAttempts` |
| **Finalidade** | Número máximo de tentativas MFA antes de bloqueio |

### Mfa:MfaLockoutMinutes

| Propriedade | Valor |
|---|---|
| **Nome** | `Mfa:MfaLockoutMinutes` |
| **Tipo** | `int` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `15` |
| **Arquivo Origem** | `appsettings.json` (linha 15) |
| **Variável Env** | `Mfa__MfaLockoutMinutes` |
| **Finalidade** | Tempo de bloqueio MFA após máximo de tentativas |

**Classe:** `MfaOptions` em `Features/Auth/Mfa/MfaConfiguration.cs`

---

## 🔐 4. TRAVA DE SENHA (Password Lockout)

### PasswordLockout:PasswordMaxAttempts

| Propriedade | Valor |
|---|---|
| **Nome** | `PasswordLockout:PasswordMaxAttempts` |
| **Tipo** | `int` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `5` |
| **Arquivo Origem** | `appsettings.json` (linha 18) |
| **Variável Env** | `PasswordLockout__PasswordMaxAttempts` |
| **Finalidade** | Número máximo de tentativas de senha antes de bloqueio |

### PasswordLockout:PasswordLockoutMinutes

| Propriedade | Valor |
|---|---|
| **Nome** | `PasswordLockout:PasswordLockoutMinutes` |
| **Tipo** | `int` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `15` |
| **Arquivo Origem** | `appsettings.json` (linha 19) |
| **Variável Env** | `PasswordLockout__PasswordLockoutMinutes` |
| **Finalidade** | Tempo de bloqueio de senha após máximo de tentativas |

**Classe:** `PasswordLockoutOptions` em `Features/Auth/PasswordLockoutOptions.cs`

---

## 🐰 5. RABBITMQ — IMPORT (Fila de Importação)

### RabbitMq:Host

| Propriedade | Valor |
|---|---|
| **Nome** | `RabbitMq:Host` |
| **Tipo** | `string` |
| **Obrigatório** | ⚠️ **SIM** (vazio por padrão) |
| **Valor Padrão** | `""` (vazio) |
| **Arquivo Origem** | `appsettings.json` (linha 67) |
| **Variável Env** | `RabbitMq__Host` |
| **Finalidade** | Hostname do servidor RabbitMQ |
| **Exemplo** | `localhost` ou `rabbitmq.docker.internal` |

### RabbitMq:Port

| Propriedade | Valor |
|---|---|
| **Nome** | `RabbitMq:Port` |
| **Tipo** | `int` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `5672` |
| **Arquivo Origem** | `appsettings.json` (linha 68) |
| **Variável Env** | `RabbitMq__Port` |
| **Finalidade** | Porta do RabbitMQ |

### RabbitMq:VirtualHost

| Propriedade | Valor |
|---|---|
| **Nome** | `RabbitMq:VirtualHost` |
| **Tipo** | `string` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `"partner"` |
| **Arquivo Origem** | `appsettings.json` (linha 69) |
| **Variável Env** | `RabbitMq__VirtualHost` |
| **Finalidade** | Virtual Host no RabbitMQ |

### RabbitMq:Username

| Propriedade | Valor |
|---|---|
| **Nome** | `RabbitMq:Username` |
| **Tipo** | `string` |
| **Obrigatório** | ⚠️ **SIM** (vazio por padrão) |
| **Valor Padrão** | `""` (vazio) |
| **Arquivo Origem** | `appsettings.json` (linha 70) |
| **Variável Env** | `RabbitMq__Username` |
| **Finalidade** | Usuário para autenticação RabbitMQ |
| **Exemplo** | `guest` |

### RabbitMq:Password

| Propriedade | Valor |
|---|---|
| **Nome** | `RabbitMq:Password` |
| **Tipo** | `string` |
| **Obrigatório** | ⚠️ **SIM** (vazio por padrão) |
| **Valor Padrão** | `""` (vazio) |
| **Arquivo Origem** | `appsettings.json` (linha 71) |
| **Variável Env** | `RabbitMq__Password` |
| **Finalidade** | Senha para autenticação RabbitMQ |
| **Exemplo** | `guest` |

### RabbitMq:Exchange

| Propriedade | Valor |
|---|---|
| **Nome** | `RabbitMq:Exchange` |
| **Tipo** | `string` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `"partner.events"` |
| **Arquivo Origem** | `appsettings.json` (linha 72) |
| **Variável Env** | `RabbitMq__Exchange` |
| **Finalidade** | Exchange para publicação de eventos |

### RabbitMq:ImportJobsQueue

| Propriedade | Valor |
|---|---|
| **Nome** | `RabbitMq:ImportJobsQueue` |
| **Tipo** | `string` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `"partner.import.jobs"` |
| **Arquivo Origem** | `appsettings.json` (linha 73) |
| **Variável Env** | `RabbitMq__ImportJobsQueue` |
| **Finalidade** | Fila para jobs de importação |

### RabbitMq:ImportDlqQueue

| Propriedade | Valor |
|---|---|
| **Nome** | `RabbitMq:ImportDlqQueue` |
| **Tipo** | `string` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `"partner.import.dlq"` |
| **Arquivo Origem** | `appsettings.json` (linha 74) |
| **Variável Env** | `RabbitMq__ImportDlqQueue` |
| **Finalidade** | Fila Dead-Letter para falhas de importação |

**Classe:** `ImportRabbitMqOptions` em `Infrastructure/Import/ImportRabbitMqOptions.cs`

---

## 🐰 6. RABBITMQ — VTEX (Fila de Sincronização VTEX)

### VtexRabbitMq:Host

| Propriedade | Valor |
|---|---|
| **Nome** | `VtexRabbitMq:Host` |
| **Tipo** | `string` |
| **Obrigatório** | ⚠️ **SIM** (padrão = localhost) |
| **Valor Padrão** | `"localhost"` |
| **Arquivo Origem** | `appsettings.json` (linha 88) |
| **Variável Env** | `VtexRabbitMq__Host` |
| **Finalidade** | Hostname do servidor RabbitMQ para VTEX |

### VtexRabbitMq:Port

| Propriedade | Valor |
|---|---|
| **Nome** | `VtexRabbitMq:Port` |
| **Tipo** | `int` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `5672` |
| **Arquivo Origem** | `appsettings.json` (linha 89) |
| **Variável Env** | `VtexRabbitMq__Port` |
| **Finalidade** | Porta do RabbitMQ para VTEX |

### VtexRabbitMq:VirtualHost

| Propriedade | Valor |
|---|---|
| **Nome** | `VtexRabbitMq:VirtualHost` |
| **Tipo** | `string` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `"partner"` |
| **Arquivo Origem** | `appsettings.json` (linha 90) |
| **Variável Env** | `VtexRabbitMq__VirtualHost` |
| **Finalidade** | Virtual Host no RabbitMQ para VTEX |

### VtexRabbitMq:Username

| Propriedade | Valor |
|---|---|
| **Nome** | `VtexRabbitMq:Username` |
| **Tipo** | `string` |
| **Obrigatório** | ⚠️ **SIM** (vazio por padrão) |
| **Valor Padrão** | `""` (vazio) |
| **Arquivo Origem** | `appsettings.json` (linha 91) |
| **Variável Env** | `VtexRabbitMq__Username` |
| **Finalidade** | Usuário para autenticação RabbitMQ VTEX |

### VtexRabbitMq:Password

| Propriedade | Valor |
|---|---|
| **Nome** | `VtexRabbitMq:Password` |
| **Tipo** | `string` |
| **Obrigatório** | ⚠️ **SIM** (vazio por padrão) |
| **Valor Padrão** | `""` (vazio) |
| **Arquivo Origem** | `appsettings.json` (linha 92) |
| **Variável Env** | `VtexRabbitMq__Password` |
| **Finalidade** | Senha para autenticação RabbitMQ VTEX |

### VtexRabbitMq:Exchange

| Propriedade | Valor |
|---|---|
| **Nome** | `VtexRabbitMq:Exchange` |
| **Tipo** | `string` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `"partner.events"` |
| **Arquivo Origem** | `appsettings.json` (linha 93) |
| **Variável Env** | `VtexRabbitMq__Exchange` |
| **Finalidade** | Exchange para eventos VTEX |

### VtexRabbitMq:VtexSyncQueue

| Propriedade | Valor |
|---|---|
| **Nome** | `VtexRabbitMq:VtexSyncQueue` |
| **Tipo** | `string` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `"partner.vtex.sync"` |
| **Arquivo Origem** | `appsettings.json` (linha 94) |
| **Variável Env** | `VtexRabbitMq__VtexSyncQueue` |
| **Finalidade** | Fila para sincronização com VTEX |

### VtexRabbitMq:VtexDlqQueue

| Propriedade | Valor |
|---|---|
| **Nome** | `VtexRabbitMq:VtexDlqQueue` |
| **Tipo** | `string` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `"partner.vtex.dlq"` |
| **Arquivo Origem** | `appsettings.json` (linha 95) |
| **Variável Env** | `VtexRabbitMq__VtexDlqQueue` |
| **Finalidade** | Fila Dead-Letter para falhas VTEX |

**Classe:** `VtexRabbitMqOptions` em `Infrastructure/Integrations/Vtex/VtexRabbitMqOptions.cs`

---

## 🌐 7. INTEGRAÇÃO VTEX

### Vtex:Enabled

| Propriedade | Valor |
|---|---|
| **Nome** | `Vtex:Enabled` |
| **Tipo** | `bool` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `false` |
| **Arquivo Origem** | `appsettings.json` (linha 80) |
| **Variável Env** | `Vtex__Enabled` |
| **Finalidade** | Feature flag: habilita/desabilita integração VTEX |
| **Nota** | **PRÉ-IMP-8: Manter como `false`** (integração real na IMP-8) |

### Vtex:BaseUrl

| Propriedade | Valor |
|---|---|
| **Nome** | `Vtex:BaseUrl` |
| **Tipo** | `string` |
| **Obrigatório** | ⚠️ **SIM quando Enabled=true** |
| **Valor Padrão** | `__REQUIRED_FROM_ENV__` |
| **Arquivo Origem** | `appsettings.json` (linha 81) |
| **Variável Env** | `Vtex__BaseUrl` |
| **Finalidade** | URL base da API VTEX |
| **Exemplo** | `https://api.vtex.com/lojabestoff` |
| **Nota** | Ignorar se Enabled=false |

### Vtex:AppKey

| Propriedade | Valor |
|---|---|
| **Nome** | `Vtex:AppKey` |
| **Tipo** | `string` |
| **Obrigatório** | ⚠️ **SIM quando Enabled=true** |
| **Valor Padrão** | `__REQUIRED_FROM_ENV__` |
| **Arquivo Origem** | `appsettings.json` (linha 82) |
| **Variável Env** | `Vtex__AppKey` |
| **Finalidade** | Chave de aplicação VTEX |
| **Nota** | Ignorar se Enabled=false |

### Vtex:AppToken

| Propriedade | Valor |
|---|---|
| **Nome** | `Vtex:AppToken` |
| **Tipo** | `string` |
| **Obrigatório** | ⚠️ **SIM quando Enabled=true** |
| **Valor Padrão** | `__REQUIRED_FROM_ENV__` |
| **Arquivo Origem** | `appsettings.json` (linha 83) |
| **Variável Env** | `Vtex__AppToken` |
| **Finalidade** | Token de aplicação VTEX |
| **Nota** | Ignorar se Enabled=false |

### Vtex:RetryCount

| Propriedade | Valor |
|---|---|
| **Nome** | `Vtex:RetryCount` |
| **Tipo** | `int` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `3` |
| **Arquivo Origem** | `appsettings.json` (linha 84) |
| **Variável Env** | `Vtex__RetryCount` |
| **Finalidade** | Número de tentativas em caso de falha |

### Vtex:RetryDelayMs

| Propriedade | Valor |
|---|---|
| **Nome** | `Vtex:RetryDelayMs` |
| **Tipo** | `int` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `1000` |
| **Arquivo Origem** | `appsettings.json` (linha 85) |
| **Variável Env** | `Vtex__RetryDelayMs` |
| **Finalidade** | Intervalo entre tentativas em milissegundos |

**Classe:** `VtexOptions` em `Infrastructure/Integrations/Vtex/VtexOptions.cs`

---

## 📤 8. IMPORTAÇÃO E ARMAZENAMENTO

### Import:StorageRoot

| Propriedade | Valor |
|---|---|
| **Nome** | `Import:StorageRoot` |
| **Tipo** | `string` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `"imports"` |
| **Arquivo Origem** | `appsettings.json` (linha 77) |
| **Variável Env** | `Import__StorageRoot` |
| **Finalidade** | Diretório raiz para armazenar arquivos de importação |
| **Nota** | Caminho relativo a partir do diretório de execução |

**Classe:** `ImportStorageOptions` em `Infrastructure/Import/ImportStorageOptions.cs`

---

## 🌍 9. CORS (Cross-Origin Resource Sharing)

### Cors:AllowedOrigins

| Propriedade | Valor |
|---|---|
| **Nome** | `Cors:AllowedOrigins` |
| **Tipo** | `string[]` (array) |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão (appsettings.json)** | `["http://localhost:4200"]` |
| **Valor Padrão (Development)** | `["http://localhost:4200"]` |
| **Arquivo Origem** | `appsettings.json` (linhas 22-24) |
| **Variável Env** | `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, etc. |
| **Finalidade** | Origins permitidas para CORS |
| **Exemplo** | `http://localhost:4200`, `https://app.example.com` |

**Configuração em:** `ServiceCollectionExtensions.cs:224` (AddCors)

---

## 📊 10. OBSERVABILIDADE — LOGGING (SERILOG)

### Serilog:MinimumLevel:Default

| Propriedade | Valor |
|---|---|
| **Nome** | `Serilog:MinimumLevel:Default` |
| **Tipo** | `string` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão (Production)** | `"Information"` |
| **Valor Padrão (Development)** | `"Debug"` |
| **Arquivo Origem** | `appsettings.json:31` / `appsettings.Development.json:9` |
| **Variável Env** | `Serilog__MinimumLevel__Default` |
| **Finalidade** | Nível mínimo de logging |
| **Valores Válidos** | `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal` |

### Serilog:MinimumLevel:Override

| Propriedade | Valor |
|---|---|
| **Nome** | `Serilog:MinimumLevel:Override` |
| **Tipo** | `object` (mapa) |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Arquivo Origem** | `appsettings.json:33-36` |
| **Sub-propriedades** | `Microsoft` (Warning → Information em Dev), `System` (Warning → Information em Dev) |
| **Finalidade** | Overrides de nível para namespaces específicos |

### Serilog:WriteTo

| Propriedade | Valor |
|---|---|
| **Nome** | `Serilog:WriteTo` |
| **Tipo** | `array` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Arquivo Origem** | `appsettings.json:38-50` |
| **Destinations** | Console, File |
| **Finalidade** | Destinos de logging |
| **File Path** | `logs/partner-api-.json` (rotação diária) |
| **Retained Files** | 7 dias |

### Serilog:Enrich

| Propriedade | Valor |
|---|---|
| **Nome** | `Serilog:Enrich` |
| **Tipo** | `array` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Arquivo Origem** | `appsettings.json:52-56` |
| **Enrichers** | FromLogContext, WithMachineName, WithEnvironmentName |
| **Finalidade** | Enriquecedores de contexto de log |

### Serilog:Properties

| Propriedade | Valor |
|---|---|
| **Nome** | `Serilog:Properties` |
| **Tipo** | `object` (mapa) |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Arquivo Origem** | `appsettings.json:57-59` |
| **Propriedades** | `Application: "Partner.Api"` |
| **Finalidade** | Propriedades globais adicionadas a todos os logs |

---

## 📡 11. OBSERVABILIDADE — SEQ (LOG CENTRALIZADO)

### Serilog:Seq:Enabled

| Propriedade | Valor |
|---|---|
| **Nome** | `Serilog:Seq:Enabled` |
| **Tipo** | `bool` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão (Production)** | `false` |
| **Valor Padrão (Development)** | `true` |
| **Arquivo Origem** | `appsettings.json:61` / `appsettings.Development.json:16` |
| **Variável Env** | `Serilog__Seq__Enabled` |
| **Finalidade** | Habilita/desabilita envio de logs para Seq |
| **Nota** | Em produção, mantém false. Em dev, true (Seq é opcional) |

### Serilog:Seq:ServerUrl

| Propriedade | Valor |
|---|---|
| **Nome** | `Serilog:Seq:ServerUrl` |
| **Tipo** | `string` |
| **Obrigatório** | ❌ NÃO (tem padrão) |
| **Valor Padrão** | `"http://localhost:5341"` |
| **Arquivo Origem** | `appsettings.json:62` |
| **Variável Env** | `Serilog__Seq__ServerUrl` |
| **Finalidade** | URL do servidor Seq |
| **Nota** | Ignorar se Serilog:Seq:Enabled=false |

### Serilog:Seq:ApiKey

| Propriedade | Valor |
|---|---|
| **Nome** | `Serilog:Seq:ApiKey` |
| **Tipo** | `string` |
| **Obrigatório** | ❌ NÃO (pode estar vazio) |
| **Valor Padrão** | `""` (vazio) |
| **Arquivo Origem** | `appsettings.json:63` |
| **Variável Env** | `Serilog__Seq__ApiKey` |
| **Finalidade** | API Key para autenticação no Seq |
| **Nota** | Apenas se Seq requer autenticação |

---

## ⚡ 12. RATE LIMITING

### Rate Limiting Policies

| Política | Limite | Janela | Partition |
|---|---|---|---|
| **AuthLogin** | 10 tentativas | 1 minuto | IP + Login |
| **AuthMfaVerify** | 20 tentativas | 1 minuto | IP + PendingTokenId |
| **AuthMfaActivate** | 10 tentativas | 1 minuto | UserId |
| **AuthMfaSetup** | 5 tentativas | 1 minuto | UserId |
| **AdminMfaReset** | 20 tentativas | 1 minuto | AdminUserId |
| **AdminMfaUnlock** | 20 tentativas | 1 minuto | AdminUserId |

**Nota:** Rate limiting é definido em código (`ServiceCollectionExtensions.cs:100-218`) e não é configurável via appsettings.

**Resposta a Rate Limit:** HTTP 429 (Too Many Requests)

---

## 🆔 13. VALIDAÇÃO DE CPF

### Feature Flags CPF

| Aspecto | Implementação | Configurável |
|---|---|---|
| **Validação Dígitos** | Exigir 11 dígitos | ❌ Não (hardcoded) |
| **Validação Algoritmo** | Algoritmo de CPF válido | ❌ Não (hardcoded) |
| **Mascaramento** | Mascarado em responses TOTP | ❌ Não (hardcoded) |
| **Hash Storage** | Não armazenado hashado | ❌ Não |
| **Feature LGPD** | Não implementado | ❌ Não |

### Localização do Código CPF

| Função | Arquivo | Linha |
|---|---|---|
| `IsValidCpf()` | `Features/Import/ImportEndpoints.cs` | Importação CSV |
| `IsValidCpf()` | `Infrastructure/Import/ImportWorker.cs` | Worker de importação |
| Validação | Ambos os arquivos executam validação idêntica |

### Erro de Validação CPF

```
CPF must contain 11 digits.
CPF is invalid.
```

**Não há configuração ou feature flags específicas para CPF** — validação é hardcoded no código.

---

## 🔗 DEPENDÊNCIAS EXTERNAS OBRIGATÓRIAS

| Dependência | Porta | Obrigatório | Propósito |
|---|---|---|---|
| **SQL Server** | 1433 | ✅ **SIM** | Database Principal |
| **RabbitMQ (Import)** | 5672 | ⚠️ Sim para importação | Fila de jobs de importação |
| **RabbitMQ (VTEX)** | 5672 | ⚠️ Condicional (se VTEX habilitado) | Fila de sincronização VTEX |
| **Seq** | 5341 | ❌ Não (opcional) | Logging centralizado |

---

## 🌐 ENDPOINTS FINAIS

| Endpoint | URL | Propósito |
|---|---|---|
| **Swagger** | `https://localhost:7111/swagger` | Documentação interativa |
| **Health Check** | `https://localhost:7111/health` | Status geral da aplicação |
| **Health Live** | `https://localhost:7111/health/live` | Verificação de disponibilidade |
| **Health Ready** | `https://localhost:7111/health/ready` | Verificação de readiness (DB) |

---

## 🔧 AMBIENTE DE EXECUÇÃO

### Variáveis de Ambiente do Sistema

| Variável | Valor Esperado | Obrigatório |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` | ✅ SIM (para dev local) |
| `ASPNETCORE_URLS` | `https://localhost:7111` | ❌ NÃO (no launchSettings) |

---

## 📝 RESUMO DE VARIÁVEIS POR CATEGORIA

### ✅ OBRIGATÓRIAS (DEVEM SER FORNECIDAS)

1. `ConnectionStrings__PartnerDb` — String conexão SQL Server
2. `Jwt__SecretKey` — Chave JWT (min 32 chars)
3. `RabbitMq__Host` — Host RabbitMQ (ou vazio para desabilitar)
4. `RabbitMq__Username` — Username RabbitMQ
5. `RabbitMq__Password` — Password RabbitMQ
6. `VtexRabbitMq__Username` — Username RabbitMQ VTEX
7. `VtexRabbitMq__Password` — Password RabbitMQ VTEX

### ⚠️ CONDICIONALMENTE OBRIGATÓRIAS

1. `Vtex__BaseUrl` — Se `Vtex__Enabled=true`
2. `Vtex__AppKey` — Se `Vtex__Enabled=true`
3. `Vtex__AppToken` — Se `Vtex__Enabled=true`

### ❌ OPCIONAIS (TÊM PADRÕES)

1. `Jwt__Issuer` — Padrão: `Partner.Api`
2. `Jwt__Audience` — Padrão: `Partner.Web`
3. `Jwt__ExpirationMinutes` — Padrão: `60`
4. `Mfa__MfaPendingTokenMinutes` — Padrão: `10`
5. `Mfa__MfaMaxAttempts` — Padrão: `5`
6. `Mfa__MfaLockoutMinutes` — Padrão: `15`
7. `PasswordLockout__PasswordMaxAttempts` — Padrão: `5`
8. `PasswordLockout__PasswordLockoutMinutes` — Padrão: `15`
9. `RabbitMq__Port` — Padrão: `5672`
10. `RabbitMq__VirtualHost` — Padrão: `partner`
11. `RabbitMq__Exchange` — Padrão: `partner.events`
12. `RabbitMq__ImportJobsQueue` — Padrão: `partner.import.jobs`
13. `RabbitMq__ImportDlqQueue` — Padrão: `partner.import.dlq`
14. `VtexRabbitMq__Host` — Padrão: `localhost`
15. `VtexRabbitMq__Port` — Padrão: `5672`
16. `VtexRabbitMq__VirtualHost` — Padrão: `partner`
17. `VtexRabbitMq__Exchange` — Padrão: `partner.events`
18. `VtexRabbitMq__VtexSyncQueue` — Padrão: `partner.vtex.sync`
19. `VtexRabbitMq__VtexDlqQueue` — Padrão: `partner.vtex.dlq`
20. `Vtex__Enabled` — Padrão: `false`
21. `Vtex__RetryCount` — Padrão: `3`
22. `Vtex__RetryDelayMs` — Padrão: `1000`
23. `Import__StorageRoot` — Padrão: `imports`
24. `Cors__AllowedOrigins__0` — Padrão: `http://localhost:4200`
25. `Serilog__MinimumLevel__Default` — Padrão: `Information` (Debug em Dev)
26. `Serilog__Seq__Enabled` — Padrão: `false` (true em Dev)
27. `Serilog__Seq__ServerUrl` — Padrão: `http://localhost:5341`
28. `Serilog__Seq__ApiKey` — Padrão: vazio

---

## 📞 NOTAS FINAIS

- Esta documentação foi gerada em **PRÉ-IMP-8** (antes da implementação VTEX)
- Mantém a integração VTEX **desabilitada** por padrão (`Vtex:Enabled=false`)
- Nenhuma regra de negócio foi alterada
- Nenhuma configuração de RabbitMQ foi alterada
- CPF segue validação hardcoded (nenhuma feature flag)
- Rate limiting é definido em código (não configurável)

---

**Última atualização:** 2026-06-04 | **Versão:** 1.0 | **Status:** Completo
