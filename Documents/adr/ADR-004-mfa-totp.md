# ADR 004 — MFA TOTP (RFC 6238)

## Status
Aceita

## Contexto
O projeto requer MFA obrigatório e compatível com apps padrão (Google/Microsoft/Authy/etc.).

## Decisão
Adotar **TOTP RFC 6238** com QR Code, segredo por usuário e recovery codes.

## Consequências
- Compatibilidade ampla com autenticadores.
- Necessidade de fluxo de reset e recovery codes.
- Lockout adicional para brute force.
