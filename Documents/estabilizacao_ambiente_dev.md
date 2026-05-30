# Estabilização do Ambiente de Desenvolvimento

## Objetivo

Separar claramente o legado (`SolutionsTools.*`) da stack moderna (`Migracao/Partner.Api` + `Migracao/Partner.Web`) e reduzir ruído de build no workspace.

## Ajustes aplicados

1. Solution moderna criada: `Partner.Modern.sln`
2. `Partner.Modern.sln` configurada para conter apenas:
   - `Partner.Api` (`Migracao/Partner.Api/Partner.Api.csproj`)
   - pasta lógica `Partner.Web` (referência organizacional para frontend)
3. VSCode configurado para priorizar a solution moderna em `.vscode/settings.json`:
   - `dotnet.defaultSolution = Partner.Modern.sln`
   - exclusão visual e de busca para `bin/`, `obj/`, `dist/`, `node_modules/`
4. `.gitignore` ampliado para ignorar artefatos de build e frontend:
   - `**/bin`, `**/obj`, `**/dist`, `**/node_modules`, `.angular`, `coverage`, logs adicionais

## Limpeza executada

Foi executada limpeza de artefatos (`bin`, `obj`, `dist`, `node_modules`, `.angular`) no workspace.

## Validações executadas

### .NET

- `dotnet restore Partner.Modern.sln` ✅
- `dotnet build Partner.Modern.sln -c Debug` ✅

### Angular

- `npm ci` em `Migracao/Partner.Web` ✅
- `npm run build` em `Migracao/Partner.Web` ✅

## Estrutura recomendada de trabalho

- `Documents/` → documentação técnica e arquitetural
- `Migracao/Partner.Api/` → backend moderno .NET 8
- `Migracao/Partner.Web/` → frontend Angular
- `SolutionsTools.*` → legado (referência/migração), sem evolução funcional nova
- `Partner.Modern.sln` → solution padrão para C# Dev Kit

## Pendências estruturais remanescentes

1. Definir estratégia de isolamento do legado no workspace (ex.: workspace multi-root com pasta moderna padrão).
2. Opcional: criar um `README` raiz com guia “como abrir o projeto moderno no VSCode”.
3. Revisar política para manutenção de logs locais (`api.log`, `web.log`) fora de versionamento.

## Riscos técnicos remanescentes

1. Dependências NPM com vulnerabilidades reportadas no `npm ci` (44 vulnerabilidades).
2. Presença de artefatos temporários em ambientes locais se processos ficarem abertos (node/dotnet lock de arquivos).
3. Ausência de repositório Git inicializado no diretório atual impede auditoria de diff via `git status`.

