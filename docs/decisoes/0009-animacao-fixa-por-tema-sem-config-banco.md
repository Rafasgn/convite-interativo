# 0009 — Animação fixa por tema; motor não configurável via banco no MVP

**Data:** 2026-08-06
**Status:** Aceita

## Contexto

Era preciso decidir se a sequência de animação varia por convite ou evento, ou se é fixa por tema, e se o motor de animação precisa ler configuração do banco de dados.

## Decisão

A animação é fixa por tema, apenas os nomes dos convidados variam entre convites do mesmo tema. Cada tema é HTML, CSS e JS estático em themes/<slug>/, e recebe os nomes via injeção simples, por exemplo um atributo data-nomes ou renderização server-side no Razor, sem configuração dinâmica vinda do banco.

## Motivação

Reduz drasticamente a complexidade do motor de animação no MVP, sem precisar de uma linguagem de configuração de animação nem de dados dirigindo timing ou sequência.

O caso de uso real no momento é um único tema, Pequeno Príncipe, com um único roteiro de animação.

## Consequências

Trocar de tema é trocar a pasta themes/<slug>/ usada, não reconfigurar uma animação existente.

Se no futuro for necessário parametrizar a animação em si, não só os nomes, isso é extensão de escopo e não é suportado hoje por design.

O motor de animação não deve conter strings ou paths específicos do tema Pequeno Príncipe, reforçando a convenção já existente no CLAUDE.md.
