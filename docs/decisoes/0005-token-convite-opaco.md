# 0005 — Token de convite opaco (12 bytes aleatórios em Base64Url)

**Data:** 2026-08-06
**Status:** Aceita

## Contexto

Cada convite gera um link único que dá acesso aos nomes e à confirmação de presença de um grupo específico. Existe preocupação real de alguém abrir o link de outro grupo por engano ou curiosidade.

## Decisão

O identificador de convite na URL pública é um token opaco não sequencial: 12 bytes aleatórios codificados em Base64Url, por exemplo x9J-4kL2mNp8Q. Não há autenticação por convidado, o token em si é o controle de acesso.

## Motivação

Não é adivinhável: 12 bytes de entropia tornam impraticável enumerar ou adivinhar links de outros convites.

É mais curto e mais amigável em URL que um GUID puro.

Evita a complexidade de login por convidado, que não se justifica para o caso de uso, já que o link já é o convite em si.

## Consequências

O campo de token deve ter índice único no banco.

O token é gerado uma vez, na criação do convite, e não muda.

Não há revogação ou expiração de link no MVP. Se necessário, é trabalho futuro.
