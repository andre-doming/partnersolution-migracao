# ✅ Validação do .env + Rate Limit Explicado

## 📋 Seu `.env` Está Completo para Partner.Api

### ✅ Variáveis Preenchidas (Partner.Api)
```
✅ ConnectionStrings__PartnerDb=Server=localhost;UID=usr_partner;PWD=Pass@2026!!!;Database=db_partner;
✅ Jwt__Issuer=Partner.Api
✅ Jwt__Audience=Partner.Web
✅ Jwt__SecretKey=0123456789abcdef0123456789abcdef0123456789=
✅ Jwt__ExpirationMinutes=60
✅ RabbitMq__Host=localhost
✅ RabbitMq__Port=5672
✅ RabbitMq__VirtualHost=partner
✅ RabbitMq__Username=admin
✅ RabbitMq__Password=Admin@123456
✅ VtexRabbitMq__* (todas preenchidas)
✅ Vtex__Enabled=true
✅ Vtex__BaseUrl=http://localhost:7001
✅ Vtex__AppKey=PMD-LOCAL-2026-APPKEY
✅ Vtex__AppToken=PMD-LOCAL-2026-APPTOKEN-FAKE
✅ Cpf__* (todas preenchidas)
✅ Cors__AllowedOrigins__0=http://localhost:4200
✅ Serilog__* (todas preenchidas)
✅ ASPNETCORE_ENVIRONMENT=Development
```

**Status**: ✅ **PRONTO PARA RODAR Partner.Api**

---

## 🔧 Variáveis Adicionais (Fake VTEX API)

O `.env` **NÃO** contém variáveis da Fake VTEX porque são específicas da API Python.

### Se quiser persistência e rate limit na Fake VTEX, adicionar ao `.env`:

```env
# ════════════════════════════════════════════════════════════════════════════════
# 🐍 FAKE VTEX API (Python)
# ════════════════════════════════════════════════════════════════════════════════

# Habilita persistência SQLite em data/fake_vtex.db (default: false = memória)
PERSIST_DATA=false

# Latência artificial em todas requisições (ms) - 0 a 30000
LATENCY_MS=0

# Rate limit (requisições por minuto) - 0 = sem limite
RATE_LIMIT_PER_MINUTE=0

# Nível de log (DEBUG, INFO, WARNING, ERROR)
LOG_LEVEL=INFO
```

**Mas é OPCIONAL**. A Fake VTEX já roda com valores padrão.

---

## 🎯 Rate Limit - O Que Foi Implementado

### ✅ SIM, Rate Limit foi implementado

**Arquivo**: `Migracao/FakeVtex.Api/routes/diagnostics_extended.py`

**Endpoint**: `POST /diagnostics/rate-limit`

### Como Usar

#### 1️⃣ Configurar Rate Limit (ex: 50 requisições/minuto)

```bash
curl -X POST http://localhost:7001/diagnostics/rate-limit \
  -H "X-VTEX-API-AppKey: test" \
  -H "X-VTEX-API-AppToken: test" \
  -H "Content-Type: application/json" \
  -d '{"requestsPerMinute": 50}'
```

**Resposta**:
```json
{"rateLimit": 50}
```

#### 2️⃣ Testar Rate Limit

Se você enviar >50 requisições em 1 minuto:
- 50 primeiras: ✅ HTTP 200
- Próximas: ❌ HTTP 429 (Too Many Requests)

```bash
# Simular ultrapassar o limite
for i in {1..60}; do
  curl -s http://localhost:7001/api/dataentities/CL/search \
    -H "X-VTEX-API-AppKey: test" \
    -H "X-VTEX-API-AppToken: test" | jq .statusCode
done
```

#### 3️⃣ Ver Status do Rate Limit

```bash
curl http://localhost:7001/diagnostics \
  -H "X-VTEX-API-AppKey: test" \
  -H "X-VTEX-API-AppToken: test" | jq
```

**Inclui**:
```json
{
  "rateLimitPerMinute": 50,
  "totalRequests": 52,
  "successCount": 50,
  "errorCount": 2
}
```

#### 4️⃣ Desabilitar Rate Limit

```bash
curl -X POST http://localhost:7001/diagnostics/rate-limit \
  -H "X-VTEX-API-AppKey: test" \
  -H "X-VTEX-API-AppToken: test" \
  -H "Content-Type: application/json" \
  -d '{"requestsPerMinute": 0}'
```

---

## 🎯 Outros Endpoints de Diagnóstico

Além do Rate Limit, há mais recursos implementados:

### 1. Reset Completo
```bash
curl -X POST http://localhost:7001/diagnostics/reset-all \
  -H "X-VTEX-API-AppKey: test" \
  -H "X-VTEX-API-AppToken: test"
```

### 2. Simular Latência (delay artificial)
```bash
# Todas requisições terão 2 segundos de delay
curl -X POST http://localhost:7001/diagnostics/latency \
  -H "X-VTEX-API-AppKey: test" \
  -H "X-VTEX-API-AppToken: test" \
  -H "Content-Type: application/json" \
  -d '{"delayMs": 2000}'
```

**Útil para testar timeout do Partner.Api!**

### 3. Forçar Modo de Falha
```bash
# Forçar erro 429 em todos endpoints
curl -X POST http://localhost:7001/diagnostics/failure-mode \
  -H "X-VTEX-API-AppKey: test" \
  -H "X-VTEX-API-AppToken: test" \
  -H "Content-Type: application/json" \
  -d '{"mode": "429"}'

# Modos: "none", "429", "500", "502", "503", "timeout"
```

### 4. Exportar Estado
```bash
curl http://localhost:7001/diagnostics/export \
  -H "X-VTEX-API-AppKey: test" \
  -H "X-VTEX-API-AppToken: test" > estado_fake_vtex.json
```

### 5. Importar Estado
```bash
curl -X POST http://localhost:7001/diagnostics/import \
  -H "X-VTEX-API-AppKey: test" \
  -H "X-VTEX-API-AppToken: test" \
  -H "Content-Type: application/json" \
  -d @estado_fake_vtex.json
```

---

## 📊 Resumo de Validação

| Componente | Status | Observação |
|-----------|--------|-----------|
| **Partner.Api** | ✅ Completo | Todas variáveis críticas preenchidas |
| **Fake VTEX API** | ✅ Funcional | Roda com defaults (memória, sem limit) |
| **Rate Limit** | ✅ Implementado | Endpoint: POST /diagnostics/rate-limit |
| **Latência** | ✅ Implementado | Endpoint: POST /diagnostics/latency |
| **Falhas** | ✅ Implementado | POST /diagnostics/failure-mode |
| **Reset** | ✅ Implementado | POST /diagnostics/reset-all |
| **Export/Import** | ✅ Implementado | GET/POST /diagnostics/export/import |

---

## 🚀 Próximo Passo

Execute:
```bash
start_partner_local.cmd
```

Seu `.env` está 100% pronto para Partner.Api + Fake VTEX!

---

**Observação**: Rate Limit é **configurável em tempo de execução** via endpoint, não precisa estar em `.env` a menos que queira valor padrão ao iniciar (ainda não implementado, mas pode ser adicionado em futuro).
