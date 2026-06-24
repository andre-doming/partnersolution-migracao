# Fake VTEX API (Local)

API simulada para desenvolvimento local do Partner.Modern, reproduzindo contratos da VTEX Master Data.

## Instalação

```bash
pip install -r requirements.txt
```

## Execução

```bash
uvicorn app:app --reload --port 7001
```

ou usar os scripts:

```bash
run.cmd
```

```powershell
./run.ps1
```

## Swagger

`http://localhost:7001/docs`

## Variáveis de ambiente

Arquivo `.env.example`:

- `FAILURE_RATE` → taxa de falhas (%). Ex.: `1` = 1%.
- `FORCED_FAILURE_MODE` → `none|429|500|502|503|timeout`.
- `LOG_LEVEL` → nível de log (`INFO`, `DEBUG`).

## Autenticação

Todos os endpoints exigem os headers abaixo. O conteúdo não é validado, apenas a presença.

```
X-VTEX-API-AppKey: <valor>
X-VTEX-API-AppToken: <valor>
```

## Simulação de falhas

- Aleatória via `FAILURE_RATE`
- Determinística via `POST /diagnostics/failure-mode`

```json
{
  "mode": "429"
}
```

## Endpoints

- `GET /health`
- `GET /api/dataentities/CL/search`
- `GET /api/dataentities/CL/documents/{id}`
- `POST /api/dataentities/CL/documents`
- `PUT /api/dataentities/CL/documents/{id}`
- `DELETE /api/dataentities/CL/documents/{id}`
- `GET /diagnostics`
- `POST /diagnostics/reset`
- `POST /diagnostics/failure-rate`
- `POST /diagnostics/failure-mode`

## Exemplos (curl)

```bash
curl -H "X-VTEX-API-AppKey: test" -H "X-VTEX-API-AppToken: test" http://localhost:7001/health
```

```bash
curl -H "X-VTEX-API-AppKey: test" -H "X-VTEX-API-AppToken: test" \
  http://localhost:7001/api/dataentities/CL/search
```

```bash
curl -X POST -H "Content-Type: application/json" \
  -H "X-VTEX-API-AppKey: test" -H "X-VTEX-API-AppToken: test" \
  -d '{"firstName":"Ana","lastName":"Silva","email":"ana@ex.com","document":"123","companyId":"1","isActive":true,"clientGuid":"guid","syncedAtUtc":"2024-01-01T00:00:00Z"}' \
  http://localhost:7001/api/dataentities/CL/documents
```

## Integração com Partner.Api

Configure no `Partner.Api`:

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

## Observações

- Dados em memória (reinicia ao parar o processo).
- Seeds iniciais em `data/seed_clients.json` (50 clientes).