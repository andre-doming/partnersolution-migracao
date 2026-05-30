# Plano incremental — Feature Clients (Clientes)

## 1) Plano incremental

1. **Baseline e contrato de dados**
   - Confirmar colunas legadas em `tb_cliente` e vínculo com `tb_empresa`.
   - Definir DTOs de listagem, detalhe e persistência para manter contrato estável.

2. **Backend leitura primeiro**
   - Implementar `GET /api/clients` com paginação SQL (`OFFSET/FETCH`) e `COUNT` separado.
   - Implementar filtros whitelist (ex.: `name`, `email`, `cpf`, `code`) + `active`.
   - Restringir resultados por empresas do usuário autenticado (quando não admin).
   - Implementar `GET /api/clients/{id}` com validação de escopo por empresa.

3. **Backend escrita**
   - Implementar `POST /api/clients` (com vínculo de empresa obrigatório).
   - Implementar `PUT /api/clients/{id}`.
   - Implementar `DELETE /api/clients/{id}` como inativação lógica (se aplicável ao legado).

4. **Regras de negócio**
   - Validação de CPF/CNPJ ou identificadores conforme legado real da tabela.
   - Validação de unicidade de campos críticos por contexto de empresa (quando aplicável).
   - Normalização de campos relevantes (documento, e-mail, telefone, etc.).

5. **Autorização por ação**
   - Policies `Clients`, `ClientsInsert`, `ClientsUpdate`, `ClientsDelete`.
   - Reuso do padrão de autorização já aplicado em Users/Companies.

6. **Frontend Clients**
   - Tela com tabela + paginação + filtros whitelist alinhados ao backend.
   - Filtro por empresa (apenas empresas permitidas ao usuário).
   - Modal Reactive Forms para create/update.
   - Ações condicionadas por permissão.

7. **Validação final**
   - `dotnet build` no backend e `npm run build` no frontend.
   - Revisão de consistência auth/authz e mensagens de erro.

8. **Preparação para futura importação CSV**
   - Garantir mesmos campos-chave no DTO de upsert e listagem.
   - Evitar regras acopladas à UI para facilitar reaproveitamento em endpoint de importação futura.

## 2) Riscos técnicos

1. **Ambiguidade de regra de unicidade**
   - No legado, algumas regras podem ser globais e outras por empresa.

2. **Dados históricos inconsistentes**
   - Documento/e-mail/telefone sem padrão único podem gerar falhas de validação.

3. **Escopo por empresa do usuário**
   - Risco de vazamento de dados se filtro por empresa não for aplicado em todos endpoints (lista/detalhe/update/delete).

4. **Drift entre front e back**
   - Campos/filtros divergentes entre UI e API podem gerar erros silenciosos de busca.

## 3) Preocupações de performance

1. **Paginação com filtros textuais**
   - `LIKE '%term%'` em colunas sem índice pode degradar em bases grandes.

2. **Filtro por empresas permitidas**
   - `IN (...)` com muitos IDs de empresa pode impactar planos de execução.

3. **COUNT + SELECT separados**
   - padrão simples e legível, porém com custo duplo por requisição; manter consultas enxutas.

4. **Ordenação estável**
   - garantir `ORDER BY` indexável para evitar variação de página e custo extra.

## 4) Simplificações recomendadas

1. **Sem query builder genérico**
   - manter SQL explícito por slice.

2. **Sem abstrações extras**
   - sem MediatR, sem Generic Repository, sem camadas artificiais.

3. **Whitelist curta e objetiva**
   - começar com filtros essenciais e evoluir só se houver necessidade.

4. **Delete lógico apenas**
   - manter histórico e reduzir risco operacional.

## 5) Reaproveitamento das features anteriores

- **Users/Companies backend**
  - padrão `Models + Queries + Validators + Endpoints`.
  - `FilterSanitizer` para whitelist e normalização de termos.
  - policies por ação em `AuthPolicies` + `ServiceCollectionExtensions`.

- **Users/Companies frontend**
  - estrutura `models + service + page + dialog`.
  - paginação Material (`MatPaginator`) e filtros reativos.
  - controle de ações por permissões em `AuthService.hasPermission`.

- **Fundação auth/authz**
  - claims padronizadas e checagem admin/permissão.
  - manutenção de compatibilidade com contrato JWT existente.

