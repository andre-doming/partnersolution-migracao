# Modernização Partner Solution

## Resumo

Modernizar incrementalmente o legado para `Migracao/Partner.Api` + `Migracao/Partner.Web`, mantendo a base SQL inicial e a identidade visual BestOff, mas removendo acoplamento de WebForms, sessão de servidor, SQL concatenado, secrets hardcoded e regras espalhadas na UI.

O legado atual contém:
- `SolutionsTools.UI`: WebForms com `aspx`, `ascx`, sessão, GridView, modais e upload CSV.
- `SolutionsTools.DAL/BLL/Services`: Dapper/Dapper.Contrib, consultas SQL com `string.Format`, MD5, integrações VTEX e envio de e-mail.
- `SolutionsTools.WebApi`: API antiga separada, token próprio e endpoints de cliente.
- Banco: `tb_usuario`, `tb_empresa`, `tb_cliente`, `tb_funcao`, vínculos usuário/empresa/função e `tb_log`.

Toda documentação formal gerada depois deve ser salva em `D:\Dev\PartnerTools\partnersolution\Documents`.

## Arquitetura Proposta

Backend:
- Usar `.NET 8`, ASP.NET Core Web API, Dapper, SQL Server, JWT, FluentValidation, Serilog e middleware global.
- Manter uma aplicação simples em um único projeto `Partner.Api`, organizada por Vertical Slice:
  - `Features/Auth`
  - `Features/Users`
  - `Features/Companies`
  - `Features/Clients`
  - `Features/Import`
  - `Infrastructure/Database`
  - `Infrastructure/Security`
  - `Infrastructure/ExternalServices`
  - `Middleware`
  - `Shared`
- Evitar Generic Repository. Cada feature terá handlers/services pequenos e queries Dapper explícitas, parametrizadas e próximas do caso de uso.
- Representar permissões como claims no JWT: `admin`, `companies`, `permissions`, `userId`, `name`, `login`.

Frontend:
- Usar Angular em `Partner.Web`, com layout autenticado contendo header, logo, menu por permissão, usuário logado, logout e footer.
- Estrutura:
  - `core`: auth, interceptors, guards, config API, theme service
  - `layout`: shell principal
  - `shared`: tabela, dialogs, form controls, snackbars
  - `features/auth`, `users`, `companies`, `clients`, `import`
- Angular Material para tabelas, dialogs, snackbars, inputs, select, radio/toggle e paginação.
- Tema claro/escuro com persistência local.

## Regras Implícitas e Melhorias

Regras preservadas:
- Usuário precisa estar ativo.
- Admin acessa tudo.
- Usuário comum depende de funções como `funcClientes`, `funcClientesUpd`, `funcEmpresasIns`, `funcUsuariosDel`.
- Usuário também é limitado por empresas relacionadas.
- `primeiro_acesso = S` exige troca de senha.
- `acesso_token = S` distingue usuário apto a token/API antiga.
- Clientes importados são vinculados a empresa, sincronizados com VTEX e gravados localmente.
- CPF é armazenado localmente como MD5 no legado; isso deve ser tratado como compatibilidade temporária.
- Empresa `CNPJ = 00000000000000` funciona como empresa destino para remoção/desvinculação.
- Importação CSV usa `;`, colunas do `modelo.csv` e ações `inserir`, `atualizar`, `excluir`.
- Logs registram usuário, cliente e movimentação `I`, `U`, `D`.

Melhorias prioritárias:
- Parametrizar 100% das queries Dapper.
- Trocar MD5 de senha por hash moderno, mantendo compatibilidade de login inicial para senhas antigas.
- Mover VTEX app key/token para configuração segura.
- Criar resposta de erro padronizada sem stack trace.
- Validar CPF, CNPJ, e-mail, CSV e permissões no backend.
- Usar paginação no banco/API, não carregar tudo em sessão.
- Criar logs de importação com status por linha.
- Corrigir nomenclaturas na camada nova sem exigir migração destrutiva imediata no banco.

## Roadmap Incremental

1. Fundação
- Criar `Partner.Api` e `Partner.Web` vazios, configuração, health check, Swagger DEV, Serilog, middleware global, CORS e estrutura base.
- Criar documento arquitetural inicial em `Documents`.

2. Autenticação e Usuários
- Implementar login JWT, leitura de usuário ativo, permissões e empresas.
- Implementar troca de senha no primeiro acesso.
- Implementar listagem/criação/edição/inativação de usuários e vínculos com empresas/funções.
- Criar guard/interceptor no Angular e layout autenticado.

3. Empresas
- Implementar listagem, filtro, cadastro, edição e inativação.
- Manter integração VTEX em serviço isolado, com timeout, logs e erro amigável.
- Preservar regra de empresa ativa e CNPJ único.

4. Clientes
- Implementar consulta por empresa, filtros, paginação, edição, aprovação e exclusão/desvinculação.
- Preservar restrição por empresas do usuário e permissões por ação.
- Reduzir chamadas VTEX em tela de listagem; buscar dados externos sob demanda ou com cache controlado.

5. Importação CSV
- Implementar upload, validação de estrutura, preview, seleção de linhas, processamento e relatório de erros.
- Processar por linha com resultado individual, sem abortar lote inteiro.
- Registrar logs de importação e movimentação.

## Riscos

- SQL injection no legado: migração deve bloquear concatenação de SQL desde o início.
- Secrets VTEX estão hardcoded: risco alto de segurança e vazamento.
- Senha MD5: precisa plano de compatibilidade e migração gradual de hash.
- Regras de permissão estão espalhadas na UI: risco de esquecer permissões se não forem centralizadas no backend.
- Integração VTEX parece fonte de verdade para parte dos dados: risco de inconsistência entre SQL local e VTEX.
- `tb_log` no script diverge do código, que usa campos como entidade/ação em alguns pontos; precisa validar schema real antes de implementar logs.
- Importação atual depende de sessão e GridView; nova versão precisa preservar comportamento sem reproduzir acoplamento.
- Alguns campos existem nos prints/código mas não no SQL local, como sexo, data nascimento e aprovado; devem ficar como dados externos/DTO até confirmar modelagem.

## Testes e Aceite

- Testes backend por feature: login, permissões, troca de senha, filtros, CRUD, validações e importação.
- Testes de segurança: usuário inativo, sem permissão, empresa não relacionada, token expirado e input malicioso em filtros.
- Testes de integração com SQL usando base de desenvolvimento.
- Testes manuais guiados pelos prints: login, home, importar, clientes, empresas, usuários e modais.
- Aceite da primeira entrega: API autentica com JWT, frontend loga, layout renderiza menus por permissão, usuários funcionam e nenhum endpoint expõe stack trace ou concatena SQL.

## Assumptions

- Manter `.NET 8` e Angular conforme planejamento existente, priorizando estabilidade e portfólio.
- Reaproveitar o banco inicialmente, sem renomear tabelas na primeira etapa.
- Não gerar código nesta etapa.
- Documentos futuros serão gravados somente em `Documents`.

## Observability
- CorrelationId middleware
- Request logging
- Tempo de execução
- Logs estruturados