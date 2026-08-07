# 0006 — Confirmação de presença editável, sem deadline no MVP

**Data:** 2026-08-06
**Status:** Aceita

## Contexto

Depois de confirmar ou recusar presença, o convidado pode mudar de ideia até a data do evento.

## Decisão

O convidado pode alterar sua resposta (confirmar ou não vai) quantas vezes quiser, enquanto o convite estiver acessível. Não há prazo-limite para resposta no MVP.

## Motivação

Simplicidade: não é necessário estado de resposta fechada nem lógica de bloqueio por data.

O caso de uso real, um evento único em 24/10/2026, não exige por ora cortar respostas antes da data.

## Consequências

A tela do convite sempre permite reabrir ou alterar a resposta.

Cada alteração deve atualizar a data de confirmação para refletir a resposta mais recente.

Se virar problema, por exemplo um prazo para fechar lista com o buffet, adiciona um campo de deadline por evento depois. Não é bloqueio de design agora.
