# Plano incremental — Feature Users (ponta a ponta)

## 1) Plano incremental da feature

1. **Contrato e baseline de dados**
   - Confirmar campos mínimos de usuário na API nova com base no legado.
   - Definir DTOs de entrada/saída para listagem, detalhe e persistência.

2. **Backend Users — leitura primeiro**
   - Implementar `GET /api/users` com paginação SQL (`OFFSET/FETCH`).
   - Implementar filtros whitelist (`name`, `login`, `email`, `active`, `companyId`).
   - Implementar `GET /api/users/{id}` com empresas/permissões vinculadas.

3. **Backend Users — escrita**
   - Implementar `POST /api/users`.
   - Implementar `PUT /api/users/{id}`.
   - Implementar `DELETE /api/users/{id}` como inativação lógica (`ativo = 'N'`).
   - Persistir vínculos `tb_empresa_usuario` e `tb_funcao_usuario` em transação.

4. **Backend Users — regras de negócio e segurança**
   - Validar unicidade de login/e-mail ativo.
   - Validar existência de empresas/funções ativas informadas.
   - Aplicar policies por ação (view/insert/update/delete).

5. **Frontend Users — lista e filtros**
   - Criar página de usuários com tabela Material paginada.
   - Filtros por campos permitidos, com request paginado.
   - Botões por permissão (`view/insert/update/delete`).

6. **Frontend Users — formulário modal**
   - Modal com Reactive Forms para criar/editar usuário.
   - Seleção múltipla de empresas/permissões.
   - Validações de UX alinhadas ao backend.

7. **Fechamento técnico**
   - Build backend/frontend.
   - Ajustes finos de mensagens de erro e consistência auth/authz.
   - Documentação final da etapa.

## 2) Estrutura proposta

### Backend (`Migracao/Partner.Api/Features/Users`)

- `UserEndpoints.cs` (rotas HTTP enxutas)
- `UserModels.cs` (DTOs Request/Response)
- `UserQueries.cs` (SQL explícito e parametrizado)
- `UserValidators.cs` (FluentValidation)

Suporte reutilizado:
- `Infrastructure/Database` (conexão)
- `Shared/Security/FilterSanitizer.cs` (whitelist/sanitização)
- `Infrastructure/Security/AuthPolicies.cs` + claims já consolidadas

### Frontend (`Migracao/Partner.Web/src/app/features/users`)

- `users.page.ts/html/scss` (tela principal)
- `users.service.ts` (HTTP da feature)
- `user-form-dialog.component.ts/html/scss` (modal create/update)
- `users.models.ts` (tipos da feature)

Integração com base existente:
- rotas em `app.routes.ts`
- menu em `layout/shell.component.ts`
- permissões via `core/auth/auth.service.ts`

## 3) Riscos técnicos

1. **Divergência de schema legado x esperado**
   - `tb_funcao`, `tb_empresa_usuario`, `tb_funcao_usuario` podem ter inconsistências de dados.

2. **Carga de listagem sem índice adequado**
   - Paginação com filtros em coluna não indexada pode degradar performance.

3. **Duplicidade de regra de permissão**
   - Se front validar diferente do backend, surgem falsas permissões/negações.

4. **Acoplamento de payload de formulário**
   - Estrutura de vínculos (empresas/permissões) precisa manter contrato estável.

## 4) Possíveis simplificações (recomendadas)

1. **Primeira entrega com “perfil completo” de permissões**
   - Trabalhar inicialmente com seleção de códigos de função existentes, sem UI complexa de agrupamento.

2. **Inativação lógica no delete**
   - Evitar exclusão física nesta etapa.

3. **Sem abstrações extras**
   - Manter SQL em `UserQueries`, endpoint direto com Dapper e validação explícita.

4. **Sem componente genérico prematuro**
   - Criar tabela/modal de Users dedicados; generalizar só com repetição real.

