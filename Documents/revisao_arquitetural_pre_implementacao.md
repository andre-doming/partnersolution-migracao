Revisão Arquitetural

A arquitetura proposta está bem calibrada para o tamanho do Partner Solution, desde que a gente segure a mão em alguns pontos. O melhor caminho é uma API .NET 8 simples, um Angular organizado por features, Dapper com SQL explícito e regras de autorização centralizadas no backend. O perigo não está na stack, está em transformar um sistema administrativo pequeno em uma “arquitetura de referência” pesada demais.

O Que Manter

Partner.Api como projeto único no backend. Nada de quebrar em múltiplos projetos agora.
Vertical Slice por domínio funcional: Auth, Users, Companies, Clients, Import.
Dapper com queries explícitas e parametrizadas.
Middleware global para exceções, mas simples: log interno + resposta padronizada.
FluentValidation para requests de entrada.
Angular com core, shared, layout e features.
JWT com claims úteis: usuário, admin, permissões e empresas.
Roadmap incremental começando por autenticação/usuários.
O Que Simplificar

Evitar “DDD completo”. Aqui basta DDD leve: nomes bons, entidades/DTOs claros e regras próximas dos casos de uso.
Evitar CQRS formal com Command, Query, Handler, Result, Mapper, Pipeline para tudo. Pode usar handlers por endpoint, mas sem ritual.
Evitar Repository, UnitOfWork, ServiceManager, BaseService, BaseRepository.
Evitar abstrair Dapper demais. Um SqlConnectionFactory já resolve.
Evitar criar um sistema sofisticado de auditoria agora. Começar registrando operações importantes e importações.
Evitar microcomponentização no Angular. Criar componentes compartilhados só depois de repetição real.
Vertical Slice

A ideia é boa, mas precisa ser prática. Cada feature pode ter:

Features/Users
  UserEndpoints.cs
  UserQueries.cs
  UserDtos.cs
  UserValidators.cs
Isso é suficiente no começo. Separar cada endpoint em dezenas de arquivos seria excesso. A regra é: se o arquivo passou a ficar difícil de ler, divide. Antes disso, mantém simples.

Segurança

Prioridade alta:

Remover SQL concatenado desde o primeiro endpoint novo.
Não reutilizar MD5 como padrão de senha.
Criar compatibilidade temporária: se o hash antigo MD5 bater no login, regravar a senha com hash moderno.
Secrets VTEX e JWT fora do código, via appsettings + variável de ambiente.
Backend sempre valida permissão. O Angular só esconde menu/botão, não decide acesso real.
Não enviar stack trace para o frontend.
Sanitizar filtros: campos pesquisáveis devem ser whitelist, nunca nome de coluna vindo livre do frontend.
JWT

JWT é adequado, mas eu simplificaria:

Access token curto, sem refresh token na primeira versão.
Logout apenas remove token do frontend.
Claims: sub, name, login, isAdmin, permissions, companies.
Se permissões mudarem, usuário precisa logar de novo na v1. Mais tarde dá para adicionar refresh/revalidação.
Não colocar dados sensíveis no token.
Refresh token, rotação e blacklist podem esperar. São bons, mas não são necessários para a primeira migração educacional.

Angular

A organização proposta está boa. Eu evitaria:

State management global tipo NgRx.
Camadas excessivas de facade/store/effects.
Design system próprio cedo demais.
Manter:

AuthService
AuthGuard
PermissionGuard simples
HttpInterceptor
ThemeService
services por feature
Reactive Forms
Angular Material
Para tabelas, usar Material Table/Paginator, mas sem criar um componente genérico universal logo no início.

Dapper

Dapper combina muito bem com o projeto. Sugestão prática:

Queries pequenas e legíveis.
Parâmetros sempre.
Métodos específicos por caso de uso.
Paginação no SQL.
Transação apenas quando houver múltiplas escritas dependentes, como salvar usuário + empresas + funções.
Nada de Dapper.Contrib no novo projeto no começo; SQL explícito dá mais clareza e controle.
Riscos Futuros

Regras de permissão ficarem duplicadas entre Angular e API.
Integração VTEX dominar a complexidade da aplicação.
Importação CSV virar um “monstro” se tentar resolver todos os cenários na primeira entrega.
Banco legado ter divergências não visíveis no script, especialmente tb_log.
A busca por “arquitetura moderna” criar abstrações demais antes do sistema pedir.
Sugestão Prática

Eu ajustaria a arquitetura para um “Vertical Slice enxuto”:

Um projeto backend.
Sem MediatR inicialmente.
Sem repository genérico.
Sem camada Application/Domain/Infrastructure formal separada em projetos.
Endpoints + validação + queries por feature.
Infra compartilhada apenas para banco, auth, logs, integração externa e erros.
Esse desenho é moderno o bastante, simples o bastante e bom para estudo porque mostra arquitetura por intenção, não por cerimônia.

