# Plano incremental — Feature Importação CSV (síncrona)

## 1) Plano incremental

1. **Baseline e contrato de importação**
   - Consolidar colunas do modelo legado (`nome;sobrenome;cpf;email;sexo;dt_nascimento;departamento;cargo;acao`).
   - Definir contrato de upload (`multipart/form-data`: arquivo + `companyId`) e contrato de retorno detalhado.

2. **Backend de controle de execução (ImportJobs)**
   - Criar tabela de controle `ImportJobs` e tabela de erros por linha `ImportJobErrors`.
   - Registrar início/fim, status, contadores, duração e usuário executor.

3. **Processamento síncrono pragmático**
   - Endpoint único de upload/processamento síncrono.
   - Validar arquivo (tipo/tamanho/UTF-8), cabeçalho e linhas.
   - Processar linha a linha sem abortar lote inteiro.

4. **Regras de negócio de importação**
   - Normalização (documento, e-mail, sexo, ação, strings).
   - Validação por linha (ação válida, CPF/e-mail, data, campos mínimos).
   - Persistência segura com SQL parametrizado (Dapper explícito).

5. **Observabilidade e retorno**
   - Logs estruturados por job e por erro de linha.
   - Medição de tempo total de processamento.
   - Resumo final: total, sucessos, erros e lista de falhas.

6. **Frontend incremental**
   - Tela simples de upload + seleção de empresa.
   - Feedback visual durante processamento.
   - Exibição de resumo e erros da última execução.
   - Histórico simples de importações.

## 2) Riscos técnicos

1. **Qualidade do CSV real**
   - Encoding, delimitador, colunas faltando e linhas malformadas.

2. **Ambiguidade de identificação de cliente**
   - Linhas com documento inválido/vazio e e-mail inconsistente.

3. **Diferenças de regra com legado**
   - Ações e validações históricas podem variar por cliente/empresa.

4. **Acúmulo de erros em importações grandes**
   - Risco de payload muito grande no retorno se houver muitas falhas.

## 3) Preocupações de performance

1. **Processamento síncrono**
   - Tempo de resposta cresce linearmente com número de linhas.

2. **Consultas por linha**
   - N+1 queries podem penalizar lotes grandes; mitigar com SQL objetivo e índices.

3. **Persistência de erros detalhados**
   - Muitos erros podem aumentar I/O; manter schema simples e paginação no histórico.

4. **Limite pragmático de arquivo**
   - Definir limite de tamanho/linhas na V1 para proteger API.

## 4) Estratégia de observabilidade (básica)

- **Logs estruturados** com `jobId`, `userId`, `companyId`, `fileName`, `elapsedMs`, `successRows`, `errorRows`.
- **Log por exceção de linha** com número da linha e motivo.
- **Métrica de duração** com `Stopwatch` por importação.
- **Persistência operacional** em `ImportJobs` e `ImportJobErrors` para auditoria funcional.

## 5) Estratégia de evolução sem reescrita

1. **Contrato estável de entrada/saída**
   - Manter DTOs de upload/resultado e histórico como fronteira de evolução.

2. **Pipeline síncrono extraível**
   - Separar parsing/validação/processamento em métodos pequenos no slice para migração futura para processamento assíncrono.

3. **ImportJobs como base de orquestração futura**
   - Tabela já preparada para receber status de fila/reprocessamento sem mudar frontend.

4. **Sem framework genérico prematuro**
   - Implementação específica de Clients CSV primeiro; generalização apenas quando houver segundo/terceiro tipo de importação real.

