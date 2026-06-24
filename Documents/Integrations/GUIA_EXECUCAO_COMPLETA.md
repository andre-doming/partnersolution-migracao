# 🚀 Guia Completo - Executar Partner.Modern com Fake VTEX API

Este guia explica como executar **localmente**:
1. **Fake VTEX API** (Python) na porta 7001
2. **Partner.Api** (C#/.NET) na porta 7111 
3. **Partner.Web** (Angular) na porta 4200

---

## 📋 Pré-requisitos

### Obrigatório
- ✅ Visual Studio Code ou Visual Studio 2022
- ✅ .NET 8 SDK (`dotnet --version`)
- ✅ Node.js 18+ (`node --version`)
- ✅ Python 3.11+ (`python --version`)
- ✅ SQL Server (Local ou Docker)
- ✅ RabbitMQ (Local ou Docker)

### Verificar instalações
```bash
# .NET
dotnet --version

# Node
node --version
npm --version

# Python
python --version

# SQL Server
sqlcmd -S localhost -U sa -Q "SELECT @@VERSION"

# RabbitMQ (se usando Docker)
docker ps | grep rabbitmq
```

---

## 🐳 Opção 1: Usar Docker para SQL Server + RabbitMQ

### Iniciar SQL Server
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Pass@2026!!!" `
  -p 1433:1433 `
  --name sqlserver `
  -d mcr.microsoft.com/mssql/server:2022-latest
```

### Iniciar RabbitMQ
```bash
docker run -d `
  --name rabbitmq `
  -p 5672:5672 `
  -p 15672:15672 `
  rabbitmq:3-management

# Management UI: http://localhost:15672 (guest/guest)
# Virtual Host 'partner' será criado automaticamente pelo Partner.Api
```

---

## 🔧 Passo 1: Configurar Variáveis de Ambiente

### Criar arquivo `.env` na raiz do projeto

```bash
cp .env.example .env
```

### Editar `.env` com valores reais

**Seção Banco de Dados:**
```env
ConnectionStrings__PartnerDb=Server=localhost;UID=sa;PWD=Pass@2026!!!;Database=db_partner;
```

**Seção JWT:**
```env
Jwt__SecretKey=seu_secret_key_de_32_caracteres_aqui_12345678
Jwt__Issuer=Partner.Api
Jwt__Audience=Partner.Web
Jwt__ExpirationMinutes=60
```

**Seção RabbitMQ (Import):**
```env
RabbitMq__Host=localhost
RabbitMq__Port=5672
RabbitMq__VirtualHost=partner
RabbitMq__Username=guest
RabbitMq__Password=guest
RabbitMq__Exchange=partner.events
RabbitMq__ImportJobsQueue=partner.import.jobs
RabbitMq__ImportDlqQueue=partner.import.dlq
```

**Seção RabbitMQ (VTEX):**
```env
VtexRabbitMq__Host=localhost
VtexRabbitMq__Port=5672
VtexRabbitMq__VirtualHost=partner
VtexRabbitMq__Username=guest
VtexRabbitMq__Password=guest
VtexRabbitMq__Exchange=partner.events
VtexRabbitMq__VtexSyncQueue=partner.vtex.sync
VtexRabbitMq__VtexDlqQueue=partner.vtex.dlq
```

**Seção VTEX (apontando para Fake VTEX):**
```env
Vtex__Enabled=true
Vtex__BaseUrl=http://localhost:7001
Vtex__AppKey=local
Vtex__AppToken=local
Vtex__RetryCount=3
Vtex__RetryDelayMs=1000
```

**Seção Proteção de CPF:**
```env
Cpf__ValidationEnabled=true
Cpf__MaskEnabled=true
Cpf__HashEnabled=false
```

**Seção CORS (para Angular):**
```env
Cors__AllowedOrigins__0=http://localhost:4200
```

**Seção Logging:**
```env
Serilog__MinimumLevel__Default=Information
Serilog__Seq__Enabled=false
ASPNETCORE_ENVIRONMENT=Development
```

---

## 🚀 Passo 2: Iniciar Fake VTEX API (Python)

### Terminal 1 - Fake VTEX API

```bash
# Navegar para diretório
cd Migracao/FakeVtex.Api

# Instalar dependências
pip install -r requirements.txt

# Opção A: Executar com desenvolvimento (reload automático)
python -m uvicorn app:app --reload --port 7001

# Opção B: Executar com persistência SQLite
set PERSIST_DATA=true
python -m uvicorn app:app --reload --port 7001

# Opção C: Usar script pronto (Windows)
.\run.cmd

# Opção D: Usar Docker
docker compose up -d
```

**Validar**:
```bash
curl -H "X-VTEX-API-AppKey: test" -H "X-VTEX-API-AppToken: test" http://localhost:7001/health
# Esperado: {"status":"healthy","clients":50,"failureRate":1,"uptimeSeconds":...}
```

**Swagger da Fake VTEX**:
```
http://localhost:7001/docs
```

---

## 🚀 Passo 3: Iniciar Partner.Api (C#/.NET)

### Terminal 2 - Partner.Api

**Opção A: Usar script de bootstrap (mais fácil)**
```bash
# Windows CMD
start_partner_local.cmd

# Windows PowerShell
.\start_partner_local.ps1

# Linux/macOS
bash start_partner_local.sh
```

**Opção B: Executar manualmente**
```bash
# Navegar para raiz do projeto
cd Migracao/Partner.Api

# Restaurar dependências
dotnet restore

# Build
dotnet build --configuration Debug

# Run (variáveis de ambiente carregam do .env via script)
# Importante: Executar do diretório raiz onde está .env
cd ../../
dotnet run --project Migracao/Partner.Api --configuration Debug
```

**Validar**:
```bash
# Health check
curl -k https://localhost:7111/health

# Swagger
https://localhost:7111/swagger/index.html

# Health Ready
curl -k https://localhost:7111/health/ready
```

**Endpoints importantes**:
```
API: https://localhost:7111
Swagger: https://localhost:7111/swagger
Health: https://localhost:7111/health
Health Ready: https://localhost:7111/health/ready
```

---

## 🌐 Passo 4: Iniciar Partner.Web (Angular)

### Terminal 3 - Partner.Web

```bash
# Navegar para diretório
cd Migracao/Partner.Web

# Instalar dependências
npm install

# Opção A: Desenvolvimento (Hot Reload)
ng serve --port 4200 --open

# Opção B: Build produção
ng build --configuration production

# Opção C: Build e servir (sem live reload)
ng build
npx http-server -p 4200 -c-1 ./dist/partner-web
```

**Validar**:
```
Abrir no navegador: http://localhost:4200
```

---

## 📊 Verificação Rápida (Todos os Serviços)

### Criar arquivo `check_services.cmd` (Windows)

```batch
@echo off
echo.
echo ════════════════════════════════════════════════════════════════════════════════
echo  VERIFICAÇÃO DE SERVIÇOS - PARTNER.MODERN
echo ════════════════════════════════════════════════════════════════════════════════
echo.

echo [1/6] Fake VTEX API (Python) - Porta 7001
curl -s -H "X-VTEX-API-AppKey: test" -H "X-VTEX-API-AppToken: test" http://localhost:7001/health >nul
if %errorlevel% equ 0 (
    echo ✅ ONLINE - http://localhost:7001/docs
) else (
    echo ❌ OFFLINE - Inicie: cd Migracao\FakeVtex.Api && python -m uvicorn app:app --reload --port 7001
)

echo.
echo [2/6] Partner.Api (C#/.NET) - Porta 7111
curl -s -k https://localhost:7111/health >nul
if %errorlevel% equ 0 (
    echo ✅ ONLINE - https://localhost:7111/swagger
) else (
    echo ❌ OFFLINE - Inicie: cd Migracao\Partner.Api && dotnet run --configuration Debug
)

echo.
echo [3/6] Partner.Web (Angular) - Porta 4200
curl -s http://localhost:4200 >nul
if %errorlevel% equ 0 (
    echo ✅ ONLINE - http://localhost:4200
) else (
    echo ❌ OFFLINE - Inicie: cd Migracao\Partner.Web && ng serve --port 4200
)

echo.
echo [4/6] SQL Server - Porta 1433
sqlcmd -S localhost -U sa -Q "SELECT 1" >nul 2>&1
if %errorlevel% equ 0 (
    echo ✅ ONLINE
) else (
    echo ❌ OFFLINE
)

echo.
echo [5/6] RabbitMQ - Porta 5672
(echo quit | nc localhost 5672) >nul 2>&1
if %errorlevel% equ 0 (
    echo ✅ ONLINE - Management: http://localhost:15672 (guest/guest)
) else (
    echo ❌ OFFLINE
)

echo.
echo ════════════════════════════════════════════════════════════════════════════════
pause
```

### Executar verificação
```bash
check_services.cmd
```

---

## 🎯 Fluxo de Início (Ordem Recomendada)

### Primeira Execução

```
Terminal 1: Fake VTEX API
$ cd Migracao/FakeVtex.Api
$ python -m uvicorn app:app --reload --port 7001
✅ Online em http://localhost:7001/docs

Terminal 2: Partner.Api
$ start_partner_local.cmd
✅ Online em https://localhost:7111/swagger

Terminal 3: Partner.Web
$ cd Migracao/Partner.Web
$ ng serve --port 4200 --open
✅ Acessa http://localhost:4200 automaticamente
```

### Execuções Subsequentes

Deixar todos rodando e:
1. Fazer alterações no código
2. Hot-reload automático em:
   - Python (Uvicorn --reload)
   - C#/.NET (dotnet watch)
   - Angular (ng serve com live reload)

---

## 🔗 URLs importantes

| Serviço | URL | Tipo |
|---------|-----|------|
| Fake VTEX API | http://localhost:7001/docs | Swagger |
| Fake VTEX Health | http://localhost:7001/health | JSON |
| Partner.Api | https://localhost:7111/swagger | Swagger |
| Partner.Api Health | https://localhost:7111/health | JSON |
| Partner.Web | http://localhost:4200 | Web App |
| RabbitMQ Management | http://localhost:15672 | Web UI (guest/guest) |
| SQL Server | localhost:1433 | Database |

---

## 🆘 Troubleshooting

### Fake VTEX API não inicia
```bash
# Verificar se porta 7001 está livre
netstat -ano | findstr :7001

# Matar processo na porta
taskkill /PID {PID} /F

# Ou usar porta diferente
python -m uvicorn app:app --port 7002
```

### Partner.Api connection string falha
```bash
# Testar conexão SQL Server
sqlcmd -S localhost -U sa -P "Pass@2026!!!"

# Se Docker: verificar se container está rodando
docker ps | grep sqlserver
```

### Partner.Web não conecta à API
```
1. Verificar CORS em `.env`:
   Cors__AllowedOrigins__0=http://localhost:4200

2. Verificar certificado HTTPS:
   curl -k https://localhost:7111/health

3. Abrir DevTools (F12) e verificar Network
```

### RabbitMQ não conecta
```bash
# Testar conexão
docker exec rabbitmq rabbitmq-diagnostics ping

# Criar virtual host se não existir
docker exec rabbitmq rabbitmqctl add_vhost partner
docker exec rabbitmq rabbitmqctl set_permissions -p partner guest ".*" ".*" ".*"
```

---

## 📝 Variáveis de Ambiente Críticas

| Variável | Crítica | Valor Recomendado |
|----------|---------|-------------------|
| ConnectionStrings__PartnerDb | ✅ SIM | `Server=localhost;UID=sa;PWD=Pass@2026!!!;Database=db_partner;` |
| Jwt__SecretKey | ✅ SIM | Mínimo 32 caracteres (usar gerador seguro) |
| Vtex__BaseUrl | ✅ SIM | `http://localhost:7001` (apontando Fake VTEX) |
| Vtex__Enabled | ✅ SIM | `true` (para usar Fake VTEX) |
| Cors__AllowedOrigins__0 | ✅ SIM | `http://localhost:4200` (para Angular) |
| RabbitMq__Host | ✅ SIM | `localhost` |
| ASPNETCORE_ENVIRONMENT | ⚠️ | `Development` |

---

## ✅ Checklist Final

- [ ] `.env` criado e configurado
- [ ] SQL Server rodando (`docker ps`)
- [ ] RabbitMQ rodando (`docker ps`)
- [ ] Fake VTEX API online (http://localhost:7001/health)
- [ ] Partner.Api online (https://localhost:7111/health)
- [ ] Partner.Web acessível (http://localhost:4200)
- [ ] RabbitMQ Management acessível (http://localhost:15672)
- [ ] Todos os 3 terminais com hot-reload ativo

---

**Status**: ✅ Pronto para desenvolvimento local completo

*Documentado em: 08/06/2026*
