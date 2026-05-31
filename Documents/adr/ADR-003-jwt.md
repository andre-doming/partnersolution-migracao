# ADR 003 — JWT para autenticação

## Status
Aceita

## Contexto
Precisa-se de autenticação stateless, compatível com SPA e permissões via claims.

## Decisão
Utilizar **JWT** com expiração curta, sem refresh token nesta fase. Claims incluem usuário, admin, permissões e empresas.

## Consequências
- Simplicidade operacional.
- Revalidação necessária após mudanças de permissões.
- Refresh token pode ser avaliado futuramente.
