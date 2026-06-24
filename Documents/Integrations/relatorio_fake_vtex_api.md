# Relatório - Fake VTEX API Local

## Resumo Executivo

✅ **Status**: Pronto para uso local  
🔗 **URL**: `http://localhost:7001`  
📚 **Swagger**: `http://localhost:7001/docs`  
🔐 **Autenticação**: Headers `X-VTEX-API-AppKey` e `X-VTEX-API-AppToken` (qualquer valor)

---

## Arquitetura

Aplicação FastAPI independente (Python 3.12+) com armazenamento em memória e simulação de falhas. Estrutura:

```
Migracao/FakeVtex.Api
├── app.py
├── auth.py
├── config.py
├── error_simulator.py
├── models.py
├── storage.py
├── requirements.txt
├── .env.example
├── README.md
├── run.cmd
├── run.ps1
├── routes/
│   ├── health.py
│   ├── clients.py
│   └── diagnostics.py
└── data/seed_clients.json
```

---

## Como Usar

### 1. Iniciar a API

```bash
cd Migracao/FakeVtex.Api
python -m uvicorn app:app --reload --port 7001
```

ou usar scripts:
```bash
run.cmd     # Windows CMD
./run.ps1   # Windows PowerShell
```

### 2. Acessar Swagger

Abra o navegador: `http://localhost:7001/docs`

### 3. Headers de Autenticação

Todos os endpoints exigem:
```
X-VTEX-API-AppKey: <qualquer-valor>
X-VTEX-API-AppToken: <qualquer-valor>
```

**Importante**: O conteúdo não é validado, apenas a presença dos headers é verificada.

### 4. Substituir URL da VTEX no Partner.Api

Configure em `appsettings.json` ou `secrets.json`:

```json
"Vtex": {
  "Enabled": true,
  "BaseUrl": "http://localhost:7001",
  "AppKey": "local",
  "AppToken": "local",
  "RetryCount": 3,
  "RetryDelayMs": 1000
}
```

---

## Endpoints Implementados

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/health` | Status da API |
| GET | `/api/dataentities/CL/search` | Listar clientes (IDs apenas) |
| GET | `/api/dataentities/CL/documents/{id}` | Obter cliente por ID |
| POST | `/api/dataentities/CL/documents` | Criar novo cliente |
| PUT | `/api/dataentities/CL/documents/{id}` | Atualizar cliente |
| DELETE | `/api/dataentities/CL/documents/{id}` | Deletar cliente |
| GET | `/diagnostics` | Status de requisições |
| POST | `/diagnostics/reset` | Resetar contadores |
| POST | `/diagnostics/failure-rate` | Configurar taxa de falhas (%) |
| POST | `/diagnostics/failure-mode` | Forçar tipo de erro |

---

## Exemplos de Payload

### Criar Cliente (POST)

```json
{
  "firstName": "Ana",
  "lastName": "Silva",
  "email": "ana@example.com",
  "document": "12345678901",
  "companyId": "1000",
  "isActive": true,
  "clientGuid": "550e8400-e29b-41d4-a716-446655440000",
  "syncedAtUtc": "2024-01-01T10:00:00Z"
}
```

### Resposta Sucesso

```json
{
  "id": "CL123456",
  "statusCode": 201,
  "message": null,
  "success": true
}
```

### Health Check

```json
{
  "status": "healthy",
  "clients": 50,
  "failureRate": 1,
  "uptimeSeconds": 123
}
```

---

## Seed Data

A API carrega automaticamente **50 clientes fictícios** do arquivo `data/seed_clients.json`.

IDs: `CL100001` até `CL100050`

Dados incluem: firstName, lastName, email, document, companyId, isActive, clientGuid, syncedAtUtc

---

## Simulação de Falhas

### Aleatória (baseado em taxa)

```bash
# Configurar taxa de 10% de falhas
curl -X POST http://localhost:7001/diagnostics/failure-rate \
  -H "X-VTEX-API-AppKey: test" \
  -H "X-VTEX-API-AppToken: test" \
  -H "Content-Type: application/json" \
  -d '{"failureRate": 10}'
```

### Determinística (força um erro específico)

```bash
# Forçar erro 429 (Too Many Requests)
curl -X POST http://localhost:7001/diagnostics/failure-mode \
  -H "X-VTEX-API-AppKey: test" \
  -H "X-VTEX-API-AppToken: test" \
  -H "Content-Type: application/json" \
  -d '{"mode": "429"}'
```

Modos válidos: `none`, `429`, `500`, `502`, `503`, `timeout`

---

## Observações Importantes

✅ **Armazenamento em memória**: dados reiniciam ao parar o processo  
✅ **Auto-seed**: 50 clientes carregam automaticamente  
✅ **Logs seguros**: não expõe AppKey/AppToken  
✅ **Falhas configuráveis**: teste resiliência do Partner.Api  
✅ **Swagger completo**: documentação automática dos endpoints  
✅ **Sem validação de conteúdo**: headers apenas precisam exist


