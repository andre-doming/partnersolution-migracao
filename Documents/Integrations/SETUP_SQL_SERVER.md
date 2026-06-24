# 🗄️ Setup SQL Server - Partner.Modern

## ❌ Problema Atual

```
Health Check: Unhealthy
Database connection failed.
```

Não consegue conectar ao SQL Server para banco `db_partner` com usuário `usr_partner`.

---

## ✅ Solução

### Opção 1: Usar Docker (Recomendado - Mais Fácil)

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Pass@2026!!!" `
  -p 1433:1433 `
  --name sqlserver `
  -d mcr.microsoft.com/mssql/server:2022-latest

# Aguarde 30 segundos para o container iniciar completamente
```

### Opção 2: SQL Server Local Instalado

Se você já tem SQL Server installed localmente, execute no **SQL Server Management Studio**:

```sql
-- 1. Criar database
CREATE DATABASE [db_partner];

-- 2. Criar usuário
USE [master];
CREATE LOGIN [usr_partner] WITH PASSWORD = 'Pass@2026!!!';

-- 3. Dar permissões
USE [db_partner];
CREATE USER [usr_partner] FOR LOGIN [usr_partner];
ALTER ROLE [db_owner] ADD MEMBER [usr_partner];
```

---

## 🧪 Testar Conexão

Após criar banco e usuário:

```bash
# Teste com sqlcmd
sqlcmd -S localhost -U usr_partner -P "Pass@2026!!!" -d db_partner -Q "SELECT 1"

# Ou com curl
curl http://localhost:5205/health
```

**Esperado quando funcionar**:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "sqlserver",
      "status": "Healthy"
    }
  ]
}
```

---

## 🐳 Docker Compose Completo

Criar arquivo `docker-compose-full.yml` na raiz:

```yaml
version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: sqlserver-partner
    environment:
      ACCEPT_EULA: Y
      SA_PASSWORD: Pass@2026!!!
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Pass@2026!!! -Q "SELECT 1"
      interval: 10s
      timeout: 5s
      retries: 5

  rabbitmq:
    image: rabbitmq:3-management
    container_name: rabbitmq-partner
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: Admin@123456
    ports:
      - "5672:5672"
      - "15672:15672"
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  sqldata:
```

Execute:
```bash
docker-compose -f docker-compose-full.yml up -d
```

---

## 📋 Checklist

- [ ] SQL Server rodando (Docker ou local)
- [ ] Database `db_partner` criado
- [ ] Usuário `usr_partner` criado com senha `Pass@2026!!!`
- [ ] Permissões db_owner concedidas
- [ ] Testar: `curl http://localhost:5205/health`
- [ ] Esperado: `"status":"Healthy"`

---

## 🔗 URLs após funcionar

| Serviço | URL |
|---------|-----|
| Partner.Api HTTP | http://localhost:5205/health |
| Partner.Api HTTPS | https://localhost:7111/health |
| Partner.Api Swagger | https://localhost:7111/swagger |
| RabbitMQ Management | http://localhost:15672 |
| Fake VTEX Swagger | http://localhost:7001/docs |

---

**Próximo passo**: Execute uma das opções (Docker ou local) e teste!
