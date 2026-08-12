# 0016 — Modo de confirmação por Convite: Grupo vs Individual

**Data:** 2026-08-12
**Status:** Aceita

## Contexto

Alguns grupos de convidados preferem confirmar presença como uma unidade única
(ex.: "a família toda confirma junto"), enquanto outros grupos têm integrantes que
querem responder cada um por si (ex.: "o marido confirma, a esposa ainda não sabe").
O modelo até aqui (ADR 0008) já suporta status individual por Convidado, mas não
havia um jeito de escolher qual fluxo de confirmação a página pública oferece.

## Decisão

Convite ganha um campo ModoConfirmacao (enum: Grupo, Individual), default Grupo.

No modo Grupo, a página pública mostra um único botão "Confirmar" que marca todos
os Convidados daquele Convite de uma vez. No modo Individual, cada Convidado tem
seu próprio botão — baseado em honra, já que o servidor não autentica qual pessoa
específica do grupo está clicando.

Em ambos os modos, o Convite continua sendo um único token compartilhado (ADR 0005).
O convidado sempre vê a lista completa do próprio grupo, nunca de outros grupos.

## Motivação

Não dá pra saber de antemão qual fluxo cada grupo vai preferir — dar essa escolha
ao admin no momento da criação do convite é mais simples do que forçar um único
comportamento pra todo mundo.

## Por que a ADR 0008 continua válida

ADR 0008 estabelece que o Status de confirmação vive no Convidado, individualmente,
não no Convite. Isso não muda. O ModoConfirmacao não move esse dado — ele apenas
determina o fluxo de UI/interação que popula esses status individuais: no modo
Grupo, uma única ação do usuário dispara uma atualização em lote (todos os
Convidados daquele Convite recebem o mesmo Status na mesma operação); no modo
Individual, cada Convidado é atualizado numa ação separada. Em ambos os casos, o
dado final gravado é sempre por Convidado, exatamente como a ADR 0008 já previa.

## Consequências

A regra de exibição "Nome (NomeDoConvite)" para desambiguar convidados sem
sobrenome é uma decisão já tomada, mas pertence à HU-05 (página pública) e HU-06
(PDF da portaria) — não faz parte desta ADR nem da HU-04.

O comportamento de confirmação em lote (modo Grupo) e os botões individuais (modo
Individual) são implementados na HU-05, não aqui — esta ADR só cobre o campo de
dados e a decisão de design.

Convites já existentes (se houver) recebem Grupo como valor padrão via
DEFAULT 'Grupo' na coluna, preservando o comportamento anterior implícito (a
UI ainda não tinha modo — Grupo é a escolha mais próxima do que já existia).
