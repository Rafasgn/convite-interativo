# 0010 — Ilustrações via GPT-image fornecidas pelo usuário; placeholders até lá

**Data:** 2026-08-06
**Status:** Aceita

## Contexto

O tema Pequeno Príncipe precisa de artes, como céu estrelado, estrela cadente, raposa, rosa e envelope. Não havia referência visual definida ainda.

## Decisão

As ilustrações finais serão geradas com GPT-image pelo próprio usuário, conforme forem ficando necessárias. Não é responsabilidade da implementação técnica produzir arte. Até as artes chegarem, a implementação usa placeholders, como SVGs simples ou retângulos coloridos, nos lugares onde as imagens do tema entrariam.

## Motivação

Desacopla o trabalho de desenvolvimento do motor e da estrutura do trabalho de produção de arte, que segue em paralelo.

Evita bloquear a implementação do fluxo técnico esperando arte finalizada.

## Consequências

A estrutura de themes/<slug>/assets/ deve prever os slots de imagem esperados, ou seja, a convenção de nomes de arquivo, mesmo antes de as artes finais existirem.

Trocar um placeholder por uma arte final deve ser só substituir o arquivo, sem mudança de código.
