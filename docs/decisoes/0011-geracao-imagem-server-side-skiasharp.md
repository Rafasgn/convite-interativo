# 0011 — Geração da imagem de divulgação server-side com SkiaSharp

**Data:** 2026-08-06
**Status:** Superseded por 0012 (revisado em 2026-08-06)

## Nota de superação

O modelo real não tem geração de imagem nenhuma. A única imagem do sistema é um
arquivo estático (`teaser.png`) por tema, igual para todos os convidados — sem nomes,
sem dados do evento, sem composição em runtime. Todo este ADR (decisão, motivação e
consequências abaixo) fica mantido como histórico de uma abordagem considerada e
descartada, não como decisão vigente. Ver ADR 0012 para o modelo atual.

## Contexto (histórico)

Cada convite precisa de uma imagem gerada automaticamente, combinando arte-base do tema com os nomes dos convidados sobrepostos, para envio manual no WhatsApp e para uso como preview de link (ver ADR 0012).

## Decisão

A composição da imagem é feita server-side com SkiaSharp: arte-base definida por configuração do tema, com nome ou nomes dos convidados desenhados em posição e fonte também definidas por configuração do tema. Sem referência visual definitiva ainda, assume-se layout centralizado e nome em destaque, a refinar quando as artes finais chegarem.

## Motivação

SkiaSharp é mais leve e rápido que ImageSharp para composição simples de texto sobre imagem, e roda bem em container Linux, alinhado à ADR 0002.

Mantém a geração determinística e dentro do processo da aplicação, sem depender de ferramentas externas, como um headless browser, só para gerar uma imagem.

## Consequências

O tema precisa expor, além dos assets visuais, uma configuração de layout de texto, como posição, fonte, tamanho e cor, usada pelo gerador de imagem.

A imagem gerada deve ser persistida em arquivo para não precisar ser recalculada a cada acesso, respeitando o local público definido na ADR 0012.
