# 📋 Plano de Evolução - Fake VTEX API (TECH-VTEX-MOCK-3)

**Status**: Em desenvolvimento  
**Data Início**: 06/05/2026

---

## 🎯 Objetivos

Transformar a Fake VTEX API de um mockup básico em um ambiente robusto de desenvolvimento, homologação e testes.

## 📋 Checklist de Tarefas

### 1. Persistência SQLite ⏳
- [ ] Adicionar SQLAlchemy ao requirements.txt
- [ ] Criar models SQLAlchemy (Client, Metric, Config)
- [ ] Implementar storage.py com fallback in-memory/SQLite
- [ ] Variável PERSIST_DATA no .env
- [ ] Migrations automáticas
- [ ] Testar init/load/save

### 2. Docker & Docker Compose ⏳
- [ ] Criar Dockerfile
- [ ] Criar docker-compose.yml
- [ ] Adicionar .dockerignore
- [ ] Expor porta 7001
- [ ] Atualizar README com instruções

### 3. Reset Completo de Ambiente ⏳
- [ ] Novo endpoint POST /diagnostics/reset-all
- [ ] Limpar métricas
- [ ] Limpar falhas
- [ ] Limpar clientes criados
- [ ] Recarregar seeds
- [ ] Retornar clientsLoaded

### 4. Simulação de Latência ⏳
- [ ] Novo endpoint POST /diagnostics/latency
- [ ] Middleware para aplicar delay
- [ ] Guardar configuração
- [ ] Validar 0-30000 ms
- [ ] Exibir em /diagnostics

### 5. Exportação de Estado ⏳
- [ ] Novo endpoint GET /diagnostics/export
- [ ] JSON com clientes + métricas + config
- [ ] Salvar em arquivo opcional

### 6. Importação de Estado ⏳
- [ ] Novo endpoint POST /diagnostics/import
- [ ] Restaurar clientes + métricas + config
- [ ] Validação de integridade

### 7. Dashboard HTML ⏳
- [ ] Novo endpoint GET /
- [ ] HTML simples (sem framework)
- [ ] Cards com: clientes, uptime, taxa falha, latência
- [ ] Links para /docs e /diagnostics

### 8. Rate Limit ⏳
- [ ] Novo endpoint POST /diagnostics/rate-limit
- [ ] Contador por minuto
- [ ] Retorna 429 ao exceder
- [ ] Exibir em /diagnostics

### 9. Métricas Expandidas ⏳
- [ ] totalRequests, totalSuccess, totalFailures
- [ ] totalTimeouts, averageResponseTimeMs
- [ ] peakResponseTimeMs, requestsLastHour

### 10. Logs Estruturados ⏳
- [ ] Adicionar logging.config
- [ ] Arquivo logs/fake-vtex.log
- [ ] Rotação diária
- [ ] RequestId, CorrelationId no log

### 11. Testes Automatizados ⏳
- [ ] pytest + fixtures
- [ ] Testes persistência
- [ ] Testes reset-all
- [ ] Testes export/import
- [ ] Testes latência
- [ ] Testes rate-limit
- [ ] Cobertura >= 70%

### 12. Documentação ⏳
- [ ] README.md atualizado
- [ ] Seção Docker
- [ ] Seção SQLite
- [ ] Exemplos de uso
- [ ] Troubleshooting
- [ ] Relatório final

---

## 📁 Arquivos a criar/modificar

### Modificações
- `requirements.txt` → adicionar SQLAlchemy, pytest
- `config.py` → PERSIST_DATA, LATENCY_MS, RATE_LIMIT
- `storage.py` → refactor com SQLite
- `.env.example` → novos parâmetros
- `README.md` → Docker, SQLite, exemplos novos
- `app.py` → middleware latência, dashboard

### Novos Arquivos
- `Dockerfile`
- `docker-compose.yml`
- `.dockerignore`
- `models_db.py` → SQLAlchemy ORM
- `logs/.gitkeep`
- `tests/conftest.py`
- `tests/test_persistence.py`
- `tests/test_features.py`
- `templates/dashboard.html`

### Documentação
- `Documents/Integrations/relatorio_fake_vtex_evolucao.md`

---

## 🔄 Timeline Estimada

| Fase | Tarefas | ETA |
|------|---------|-----|
| 1 | SQLite + Docker | 30 min |
| 2 | Reset + Latência + Rate Limit | 20 min |
| 3 | Export/Import + Dashboard | 20 min |
| 4 | Métricas + Logs | 15 min |
| 5 | Testes | 30 min |
| 6 | Documentação | 15 min |

---

## ✅ Critérios de Sucesso

- [x] API continua rodando em :7001
- [x] Todos os endpoints VTEX funcionam
- [ ] Persistência SQLite funciona
- [ ] Docker compose sobe sem erros
- [ ] Reset-all limpa tudo
- [ ] Latência é aplicada
- [ ] Rate limit bloqueia corretamente
- [ ] Dashboard mostra dados
- [ ] Testes passam com >= 70% cobertura
- [ ] README atualizado com exemplos

---

Início: 7:17 PM
Fim: TBD
