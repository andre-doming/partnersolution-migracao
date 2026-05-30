# Plano incremental — Feature Companies (Empresas)

## 1) Plano incremental

1. **Baseline e contrato**
   - Confirmar campos de empresa usados no legado (`cnpj`, `nome_fantasia`, `razao_social`, `gerente_responsavel`, `ativo`).
   - Definir DTOs claros de listagem, detalhe e persistência.

2. **Backend leitura primeiro**
   - Implementar `GET /api/companies` com paginação SQL (`OFFSET/FETCH`).
   - Implementar filtros whitelist (`nome_fantasia`, `razao_social`, `cnpj`, `gerente_responsavel`, `ativo`).
   - Implementar `GET /api/companies/{id}`.

3. **Backend escrita**
   - Implementar `POST /api/companies`.
   - Implementar `PUT /api/companies/{id}`.
   - Implementar `DELETE /api/companies/{id}` como inativação lógica.

4. **Regras de negócio**
   - Validar CNPJ (formato e dígitos verificadores).
   - Validar unicidade de CNPJ entre empresas ativas.
   - Garantir normalização de CNPJ (somente dígitos).

5. **Autorização por ação**
   - Policies: view/insert/update/delete para Companies.
   - Reaproveitar padrão de policies aplicado em Users.

6. **Frontend Companies**
   - Listagem com tabela Material + paginação.
   - Filtros simples, alinhados ao whitelist backend.
   - Modal Reactive Forms para create/update.
   - Botões por permissão de ação.

7. **Validação final**
   - Build backend e frontend.
   - Revisão de consistência auth/authz e mensagens de erro.

## 2) Reuso direto da feature Users

- **Estrutura Vertical Slice**: `Endpoints + Models + Queries + Validators`.
- **Padrão Dapper explícito** com SQL parametrizado.
- **Paginação padrão** (`page`, `pageSize`, `total`, `items`).
- **Filtro whitelist** com `FilterSanitizer` e seleção de campos permitidos.
- **Policies por ação** no backend e checks por permissão no frontend.
- **Frontend de feature** com:
  - `*.models.ts`
  - `*.service.ts`
  - `*.page.ts/html/scss`
  - `form-dialog.component.ts/html/scss`

## 3) Riscos técnicos

1. **Dados legados inconsistentes**
   - CNPJ antigo com máscara/formato incorreto.
2. **Unicidade histórica**
   - Pode haver duplicidade de CNPJ em registros antigos inativos/ativos.
3. **Drift entre validação front e back**
   - Evitar regras diferentes para CNPJ e filtros.
4. **Perfomance em listagem**
   - Filtro por texto em colunas sem índice pode pesar em bases grandes.

## 4) Simplificações recomendadas

1. **Inativação lógica no delete** (sem exclusão física).
2. **Sem integração VTEX nesta etapa** (deixar isolada para próxima iteração).
3. **Sem componentes genéricos prematuros** (tela/modal específicos de Companies).
4. **Validação CNPJ centralizada no backend** e uma validação leve no frontend só para UX.

