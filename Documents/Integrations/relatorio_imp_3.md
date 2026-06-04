# IMP-3 — UX de Histórico/Detalhe de Importações

## Resumo
Fase dedicada à camada de UX da importação assíncrona. Foram disponibilizadas telas para histórico, detalhe da importação, listagem de erros, barra de progresso e paginação, consumindo os endpoints assíncronos existentes. Esta fase consolida a visualização operacional da importação.

---

## 1) Endpoints utilizados

- **GET** `/api/import/jobs`
- **GET** `/api/import/jobs/{jobPublicId}`
- **GET** `/api/import/jobs/{jobPublicId}/errors`

Arquivos:
- `Migracao/Partner.Api/Features/Import/ImportEndpoints.cs`

---

## 2) Telas Angular

### Histórico de importações
- Tabela com jobs, status, progresso, processadas, erros e data.
- Filtros por status e data.

### Detalhe da importação
- Resumo: status, duração, total, sucesso, erros, timestamps.
- Barra de progresso com percentual.

### Listagem de erros
- Tabela paginada com erros por linha e campos principais.

Arquivos:
- `Migracao/Partner.Web/src/app/features/import/import.page.html`
- `Migracao/Partner.Web/src/app/features/import/import.page.ts`
- `Migracao/Partner.Web/src/app/features/import/import.page.scss`

---

## 3) Componentes/Elementos

- `mat-table` para histórico e erros
- `mat-paginator` para paginação
- `mat-progress-bar` para progresso
- `mat-chip` para status

---

## 4) Barra de progresso

- Exibição de percentual no histórico.
- Exibição de percentual + contadores no detalhe.

---

## 5) Paginação

- Paginação no histórico de jobs.
- Paginação na listagem de erros.

---

## 6) Testes executados

```
dotnet build Partner.Modern.sln
dotnet test Partner.Modern.sln
```

> Resultado observado: build e testes executados com sucesso na fase anterior (IMP-2). Nesta fase, não houve testes adicionais específicos registrados além do pipeline padrão.

---

## 7) Limitações da fase (mantidas)

- Sem notificações persistidas.
- Sem SignalR.
- Sem download de erros no frontend.
- Sem integração VTEX.

***

**Documento IMP-3 finalizado.**