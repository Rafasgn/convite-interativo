# 0007 — Dados do convidado no MVP: apenas nome

**Data:** 2026-08-06
**Status:** Aceita

## Contexto

Havia dúvida sobre quais campos capturar por convidado, como telefone, restrição alimentar ou faixa etária.

## Decisão

No MVP, a entidade Convidado tem apenas nome. Nenhum campo especulativo é adicionado para quando precisar.

## Motivação

Nenhum desses dados é necessário para o fluxo atual: animação, confirmação e exportação para a portaria.

Evita modelar e manter campos sem uso real, na linha de não fazer over-engineering para o MVP.

## Consequências

Se um campo adicional, como restrição alimentar, virar necessidade real, é uma migration nova e simples de adicionar depois. Não precisa ser antecipado agora.
