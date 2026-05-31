# ADR 002 — Vertical Slice Architecture

## Status
Aceita

## Contexto
O projeto precisa ser claro para estudo e manutenção, evitando camadas artificiais e excesso de boilerplate.

## Decisão
Adotar **Vertical Slice** por domínio funcional (Auth, Users, Companies, Clients, Import), mantendo endpoints, queries e validações próximos do caso de uso.

## Consequências
- Melhor coesão por feature.
- Redução de camadas artificiais.
- Facilita rastrear fluxos ponta a ponta.
