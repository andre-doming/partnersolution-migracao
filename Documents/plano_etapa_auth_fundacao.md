# Plano incremental — Fundação de Autenticação/Autorização

## Objetivo da etapa

Consolidar autenticação e autorização fim a fim (backend + frontend) antes da implementação das features de negócio, mantendo arquitetura enxuta (Vertical Slice sem camadas extras).

## Plano incremental

1. **Padronizar contrato de claims JWT**
   - Definir nomenclatura única para claims no backend.
   - Alinhar frontend para consumir os mesmos nomes.

2. **Compatibilidade de senha legado + segurança moderna**
   - Manter verificação MD5 apenas para compatibilidade.
   - Implementar hash moderno (PBKDF2) como padrão.
   - Regravar automaticamente hash moderno após login válido com hash legado.

3. **Autorização simples e explícita**
   - Criar policies simples por domínio (`Users`, `Companies`, `Clients`, `Import`).
   - Regra de bypass para admin.
   - Aplicar policies nos grupos de endpoint.

4. **Pipeline e configuração segura**
   - Validar configuração JWT mínima (secret/issuer/audience).
   - Evitar aceitar secret default em ambiente não-desenvolvimento.

5. **Sanitização mínima de filtros**
   - Criar utilitário de whitelist de campos para filtros dinâmicos.
   - Criar normalização mínima de termos textuais.

6. **Frontend auth flow consistente**
   - Decodificar claims JWT de forma centralizada.
   - Ajustar guards para autenticação + permissão real.
   - Controlar menu por claims.
   - Respeitar expiração do token para sessão local.

7. **Validação final da etapa**
   - Restore/build backend.
   - Build frontend.

## Riscos desta etapa

1. **Divergência de contrato de claims** entre token emitido e token lido.
2. **Falso negativo de autenticação** se houver inconsistência entre hash legado armazenado e estratégia de fallback.
3. **Bloqueio de acesso indevido** se policies forem aplicadas sem considerar admin.
4. **Quebra de UX** se sessão expirar sem tratamento claro no frontend.

## Pontos críticos de segurança

1. **MD5 apenas transitório**: não usar para novos cadastros/alterações.
2. **Secret JWT**: exigir chave adequada e impedir secret default fora de desenvolvimento.
3. **Autorização no backend**: frontend não decide acesso real.
4. **Sanitização de filtros**: campo de ordenação/filtro deve vir de whitelist, nunca direto do cliente.
5. **Token mínimo**: não incluir dados sensíveis no JWT.

