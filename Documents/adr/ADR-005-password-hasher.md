# ADR 005 — Password Hasher oficial

## Status
Aceita

## Contexto
O legado utiliza MD5 para senhas. É necessário migrar para hash moderno sem bloquear usuários.

## Decisão
Adotar o **PasswordHasher** oficial (.NET) com fallback para MD5 apenas na autenticação inicial.

## Consequências
- Melhoria imediata de segurança para novos logins.
- Necessidade de migração gradual no login.
- Compatibilidade temporária com hashes antigos.
