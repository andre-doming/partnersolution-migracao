# Revisão de Segurança — Partner Solution v2.0

**Escopo:** análise arquitetural, recomendações e roadmap evolutivo **sem implementação**.  
**Restrições atendidas:** sem alteração de banco, endpoints, MFA ou autenticação atual.

## 1) Sumário Executivo

O Partner Solution v2.0 já possui uma base sólida de segurança: .NET 8, JWT, PBKDF2 (ASP.NET Identity PasswordHasher), migração automática de MD5, MFA TOTP, recovery codes, lockout (senha e MFA), auditoria, CorrelationId, observabilidade e RBAC por permissions.  
Os próximos ganhos relevantes estão menos em “trocar tecnologia” e mais em **hardening e operação**: rate limiting, políticas de recuperação/rotação, melhorias de auditoria e retenção, endurecimento de setup MFA e tratamento de segredos em repouso.

**Prioridades sugeridas (macro):**
1. **Rate limiting e hardening de MFA** (reduz superfície de brute force)  
2. **Auditoria operacional + correlação de eventos** (melhor resposta a incidentes)  
3. **Segurança de segredos TOTP em repouso** (reduz impacto em caso de vazamento de banco)  
4. **Revisão de estratégia de tokens/sessões** (caso haja necessidade operacional)  
5. **Avaliação de hashing com Argon2id** (benefício real existe, mas esforço e risco precisam ser pesados)

---

## 2) Contexto Atual (Baseline de Segurança)

**Já implementado:**
- .NET 8
- JWT (access token)
- PasswordHasher<TUser> do ASP.NET Core Identity (PBKDF2)
- Migração automática MD5 → hash moderno
- MFA TOTP (RFC 6238)
- Recovery Codes (hash + single-use)
- Lockout de senha
- Lockout MFA
- Auditoria com CorrelationId
- Observabilidade
- RBAC por permissions

**Premissas adicionais:**
- `tb_usuario.senha` já suporta hashes modernos longos
- Rehash automático já funcional

---

# 3) Avaliações por Tópico

## 3.1 Password Hashing

### Estado atual
**ASP.NET Core Identity PasswordHasher (PBKDF2)** é adequado para a maioria dos cenários corporativos, com boa maturidade e suporte oficial, mas não é o topo do estado da arte em resistência a GPU/ASIC (onde Argon2id se destaca).

### Comparativo resumido

| Algoritmo | Vantagens | Desvantagens | Observação prática |
|---|---|---|---|
| **PBKDF2** | Padrão consolidado, suporte nativo, fácil manutenção | Menos resistente a GPU do que Argon2id | Boa escolha com parâmetros fortes |
| **Argon2id** | Melhor resistência a GPU/ASIC, memória-hard | Mais complexidade operacional, libs externas | Melhor opção técnica hoje |
| **bcrypt** | Amplo suporte, simples | Limites de custo e memória, mais antigo | Compatível, porém inferior ao Argon2id |
| **scrypt** | Memória-hard, bom histórico | Complexidade maior que bcrypt, menos padrão no .NET | Boa, mas menos comum |

### Vale migrar para Argon2id?
**Recomendação:** **opcional**, com ganho real, mas não urgente.  
Para o perfil atual do Partner Solution, o **benefício é real, porém incremental**, e precisa ser equilibrado contra esforço e risco.

**Ganho real:**
- Maior resistência a ataques offline em caso de vazamento de hash
- Proteção extra contra GPU/ASIC

**Esforço:**
- Adotar biblioteca confiável (ex.: libs bem mantidas e auditadas)
- Ajustar PasswordHasher e compatibilidade de migração gradual
- Testes de performance e ajustes de parâmetros

**Risco:**
- Erro de parametrização (custos muito altos / DoS por CPU)
- Dependência de biblioteca externa
- Migração mal planejada afetando login

### Estratégia de migração gradual (se aprovada)
1. **Introduzir Argon2id como formato novo** (sem quebrar hashes existentes).  
2. **Identificar o tipo de hash** (prefixo/metadata).  
3. Em login válido PBKDF2, **rehash automático para Argon2id**.  
4. **Observabilidade e métricas** do custo de hash em produção.  
5. Após período de transição, considerar PBKDF2 somente para compatibilidade.

**Conclusão:** manter PBKDF2 com parâmetros fortes é aceitável. Argon2id é uma evolução desejável, mas não crítica se o foco for custo/risco baixo.

---

## 3.2 Rate Limiting (IP / Login / Usuário)

### Recomendação de abordagem
Para este projeto, o melhor é **rate limiting combinado por IP + login + usuário**, aplicado nos endpoints sensíveis:
- `/auth/login`
- `/auth/mfa/verify`
- `/auth/mfa/setup/*`
- `/auth/mfa/activate`
- endpoints administrativos de MFA

### Onde implementar
- **Aplicação (.NET)**: permite granularidade por endpoint e por identidade
- **Gateway/Reverse Proxy** (se existir): complementa com limite global por IP

### Impacto operacional
- Requer ajustes de thresholds com dados reais
- Pode bloquear usuários em redes NAT (ex.: empresas) — precisa de tuning
- Deve incluir logging e métricas para análise de abuso

**Prioridade:** alta (reduz brute force e abuso automatizado)

---

## 3.3 MFA Hardening

### Recomendações
1. **Lockout no /mfa/activate**  
   - hoje o lockout só existe no verify/login, mas o activation também precisa controle contra brute force.
2. **Proteção durante setup**  
   - limitar tentativas de confirmação de TOTP durante setup
3. **Expiração e rotação de recovery codes**  
   - impor validade por tempo (ex.: 90/180 dias)
   - regenerar obrigatoriamente após uso
4. **Política de recadastro obrigatório**  
   - definir condições para exigir recadastro (ex.: reset por admin, recovery usado, inatividade)

**Prioridade:** alta (reduz risco de bypass de MFA)

---

## 3.4 Sessões e Tokens

### Avaliação do JWT atual
JWT com expiração curta é adequado e simples.

### Refresh Token: necessário?
**Não é obrigatório** para o estágio atual.  
Só agrega valor real se:
- sessões longas forem exigência de produto
- mudanças de permissão precisarem revogação rápida
- houver necessidade de logout global imediato

### Invalidação de sessões / logout global
**Valor real:** relevante apenas se existir exigência operacional forte (compliance).  
**Overengineering:** adicionar blacklist/refresh/rotating tokens sem necessidade real.

**Recomendação:** manter JWT simples, e só evoluir para refresh/blacklist se houver demanda comprovada.

---

## 3.5 Auditoria e Segurança Operacional

### Eventos já auditados (baseline)
- MFA_ENABLED, MFA_RESET, MFA_FAILED, MFA_LOCKED
- USER_UNLOCKED
- RECOVERY_CODE_USED

### Eventos recomendados (faltantes)
- LOGIN_SUCCESS / LOGIN_FAILED (com parcimônia)
- MFA_SETUP_STARTED / MFA_SETUP_CONFIRMED
- MFA_VERIFY_SUCCESS
- PASSWORD_LOCKED / PASSWORD_UNLOCKED
- ALTERAÇÃO DE PERMISSIONS (admin)
- TROCA/RESET DE SENHA
- ALTERAÇÕES DE USUÁRIO ADMINISTRATIVO

### Retenção de logs
- Definir política formal (ex.: 6–12 meses online + 2–5 anos arquivado)
- Definir expurgo planejado (LGPD/privacidade)

### Correlação entre eventos
- Garantir **CorrelationId único por fluxo** (login + MFA + ação administrativa)
- Incluir metadata: IP, UserAgent, ActorUserId

**Prioridade:** média-alta (para resposta a incidentes)

---

## 3.6 Segurança de Banco (Secrets TOTP)

### Estado atual
Secret TOTP criptografado (conforme documentação atual).

### Recomendação
Padronizar estratégia de **criptografia em repouso**:
- **Data Protection API do ASP.NET Core** (padrão recomendado)
- Chaves protegidas por DPAPI ou Azure Key Vault (se houver infraestrutura)

**Objetivo:** reduzir impacto de vazamento de banco, garantindo que segredos TOTP não possam ser usados sem acesso às chaves da aplicação.

**Prioridade:** média

---

## 3.7 Benchmark de Mercado

### Comparação por porte

**Pequenas empresas (baseline):**
- Normalmente têm apenas login/senha e JWT simples
- MFA raramente obrigatório
- Auditoria limitada

**Partner Solution v2.0 vs pequenas empresas:** **acima da média**

**Médias empresas:**
- MFA em sistemas críticos
- Logs e auditoria consistentes
- Rate limiting básico

**Partner Solution v2.0 vs médias empresas:** **nível competitivo, mas faltam rate limiting e hardening operacional**

**Grandes empresas:**
- MFA obrigatório
- Rate limiting robusto
- Sessões revogáveis e refresh tokens
- Rotação de segredos e políticas rígidas
- Auditoria e SIEM integrados

**Partner Solution v2.0 vs grandes empresas:** **médio**, com gaps operacionais e controles avançados

### Classificação de maturidade
- **Maturidade atual:** Média (bom nível técnico, falta hardening operacional)
- **Nível de segurança atual:** Médio/alto para o porte
- **Principais gaps remanescentes:** rate limiting, MFA hardening completo, retenção de logs e políticas de segredos

---

# 4) Roadmap Evolutivo (2–3 anos)

## Fase 1 (0–6 meses) — Hardening imediato
**Objetivo:** reduzir brute force e melhorar rastreabilidade.

Prioridades:
1. Rate limiting por IP + login + usuário nos endpoints críticos
2. Lockout também em `/mfa/activate` e setup MFA
3. Expiração/rotação de recovery codes
4. Auditoria com eventos adicionais e correlação completa

Risco mitigado: brute force e abuso automatizado.

---

## Fase 2 (6–18 meses) — Segurança operacional
**Objetivo:** fortalecer proteção de segredos e capacidade de resposta.

Prioridades:
1. Criptografia de secrets TOTP com Data Protection (e gestão de chaves)
2. Política formal de retenção de logs
3. Revisão de políticas de recadastro MFA (ex.: após recovery)
4. Análise de necessidade de refresh tokens (se houver demanda real)

---

## Fase 3 (18–36 meses) — Evolução avançada
**Objetivo:** alinhar com padrões de empresas maiores (se necessário).

Prioridades:
1. Argon2id (migração gradual) se risco/benefício justificar
2. Logout global / revogação de sessões (se for requisito)
3. Integração com SIEM/monitoramento avançado
4. Hardening adicional (alertas de risco, detecção de anomalia)

---

# 5) Prioridades & Riscos

### Prioridades
1. Rate limiting e hardening MFA
2. Auditoria + retenção + correlação
3. Proteção de segredos em repouso
4. Avaliar necessidade real de refresh token / logout global
5. Argon2id (apenas se o risco/benefício justificar)

### Riscos principais se não evoluir
- Brute force em endpoints sensíveis
- Menor capacidade de investigação de incidentes
- Exposição de segredos TOTP em caso de vazamento de banco

---

# 6) Conclusão

O Partner Solution v2.0 já está **acima da média** para projetos do mesmo porte.  
As próximas evoluções mais valiosas não são “novas tecnologias”, mas **endurecimento operacional**, especialmente em rate limiting, MFA setup e auditoria.  
Argon2id é um upgrade técnico real, porém **não urgente**, e deve entrar no roadmap apenas se o risco justificar o esforço.

---

**Documento gerado sem alterações de código.**