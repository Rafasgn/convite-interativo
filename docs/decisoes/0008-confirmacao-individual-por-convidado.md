# 0008 — Confirmação de presença é individual por convidado

**Data:** 2026-08-06
**Status:** Aceita

## Contexto

Um convite pode representar um grupo, como um casal ou uma família, recebendo um único link. Era preciso decidir se a confirmação é do convite inteiro ou de cada pessoa dentro dele.

## Decisão

A confirmação é individual por convidado, mesmo quando vários convidados compartilham o mesmo convite ou link. Por exemplo, o marido confirma e a mulher não vai, e ambos os status coexistem no mesmo convite.

## Motivação

Reflete a realidade: nem todo mundo de um grupo confirma junto.

A exportação para a portaria precisa desse nível de detalhe por pessoa.

## Consequências

O status de confirmação (Confirmado, Não vai, Sem resposta) e a data da resposta vivem na entidade Convidado, não no Convite.

Na tela pública do convite, cada nome exibido tem seu próprio controle de resposta.

A confirmação do convite como conceito agregado, se precisar de um resumo por convite, é derivada a partir do status dos convidados, não um campo próprio.
