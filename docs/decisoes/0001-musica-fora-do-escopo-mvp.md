# 0001 — Música fora do escopo do MVP

**Data:** 2026-08-06
**Status:** Aceita

## Contexto
A visão inicial do projeto (`Projeto_Convite_Interativo_Visao_Inicial.pdf`) previa música
como parte da experiência de animação do convite (junto com céu estrelado, estrela
cadente, raposa, rosa e envelope).

## Decisão
A trilha sonora/música é retirada do escopo inicial (MVP). A animação do convite deve
funcionar sem depender de áudio.

## Motivação
- Reduz complexidade técnica do motor de animação no MVP (sem sincronismo áudio/animação,
  sem lidar com bloqueio de autoplay em navegadores, sem questões de licenciamento de
  trilha).
- Mantém o foco no roadmap essencial: cadastro, geração de link, animação visual,
  confirmação de presença e exportação — dentro do prazo do primeiro evento real
  (24/10/2026, envio dos convites a partir de ~20/09/2026).

## Consequências
- O motor de animação (`themes/<slug>/`) não precisa prever hooks de áudio na primeira
  versão.
- Música pode ser reavaliada como incremento futuro, inclusive por tema (nem todo tema
  precisaria ter música).
