# Inventário de Contratos VTEX (Partner.Modern)

Este documento consolida os contratos identificados no código atual da integração VTEX.

## Visão Geral do Fluxo

```
Import
  ↓
RabbitMQ
  ↓
VtexSyncWorker
  ↓
VtexClient
  ↓
VTEX
```

## Configuração e Headers

**Headers enviados (HttpClient configurado em `ServiceCollectionExtensions`):**

- `X-VTEX-API-AppKey` (valor vindo de `Vtex:AppKey`)
- `X-VTEX-API-AppToken` (valor vindo de `Vtex:AppToken`)

**Base URL:**

- `Vtex:BaseUrl`
- Exemplo: `https://api.vtex.com/lojabestoff`

## Endpoints Identificados

### 1) Health Check VTEX

- **Método:** `GET`
- **URL:** `{BaseUrl}/api/dataentities/CL/search?_fields=id&_where=id=1`
- **Objetivo:** validar conectividade/autenticação com a VTEX.

**Headers:**

- `X-VTEX-API-AppKey`
- `X-VTEX-API-AppToken`

**Status HTTP tratados:**

- `200 OK` → `Connected`
- `401 Unauthorized` → `Unauthorized`
- `403 Forbidden` → `Forbidden`
- `404 Not Found` → `NotFound`
- `>= 500` → `Disconnected`
- Timeout → `Timeout`

### 2) Sincronização de Cliente (Master Data)

Atualmente o envio real ainda é **stub** no `VtexClient.SyncClientAsync`, porém o contrato esperado foi definido pelos DTOs.

#### Request DTO (`VtexClientSyncRequest`)

```json
{
  "firstName": "string",
  "lastName": "string",
  "email": "string",
  "document": "string",
  "companyId": "string",
  "isActive": true,
  "clientGuid": "string",
  "syncedAtUtc": "2024-01-01T00:00:00Z"
}
```

#### Response DTO (`VtexClientSyncResponse`)

```json
{
  "id": "string",
  "statusCode": 200,
  "message": "string",
  "success": true
}
```

### 3) Endpoint interno (Partner.Api) para diagnóstico

No Partner.Api, há um endpoint de diagnóstico para o health check de VTEX:

- **Método:** `GET`
- **URL:** `/api/integrations/vtex/health`
- **Autenticação:** `AuthPolicies.Import` (interno)

## Observações

- A integração real com Master Data está planejada, mas o fluxo atual registra logs e retorna sucesso stub.
- O health check é o único endpoint atualmente chamado explicitamente pela integração real.
- O formato de clientes (Master Data) está baseado nos DTOs `VtexClientSyncRequest/Response`.
