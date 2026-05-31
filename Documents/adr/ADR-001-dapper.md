# ADR 001 — Dapper ao invés de EF Core

## Status
Aceita

## Contexto
O projeto precisa manter compatibilidade com o schema legado, com SQL explícito e mínimo acoplamento. É importante ter controle fino sobre queries, performance e compatibilidade.

## Decisão
Adotar **Dapper** como ORM micro para acesso a dados, escrevendo SQL explícito e parametrizado por caso de uso.

## Consequências
- Mais controle sobre consultas e performance.
- Menos automação de migrations e tracking (trade-off aceitável nesta fase).
- Evita abstrações excessivas e favorece clareza para estudo.
