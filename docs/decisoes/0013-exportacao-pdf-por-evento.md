# 0013 — Exportação para a portaria em PDF, um documento por evento

**Data:** 2026-08-06
**Status:** Aceita

## Contexto

Era preciso definir formato e granularidade da exportação da lista nominal usada na portaria do evento. A lista é usada como documento impresso no dia do evento, para conferência de quem chega, não como planilha para análise posterior.

## Decisão

Exportação em PDF, gerado com QuestPDF, um documento por evento inteiro. Contém apenas os convidados que confirmaram presença; quem recusou ou não respondeu não entra no documento. A ordenação é alfabética pelo nome do convidado, sem agrupamento por convite; o nome do convite pode aparecer como informação secundária ao lado do nome, para desambiguar homônimos, mas não define a ordem. O layout base tem um cabeçalho com nome do evento e data no topo, seguido da lista de nomes confirmados. Detalhes visuais, como colunas exatas e espaço para check manual da portaria, ficam como a refinar.

## Motivação

PDF é o formato certo para um documento impresso e conferido manualmente na portaria, diferente de CSV, que é pensado para análise em planilha.

QuestPDF tem API fluente em C#, roda em Linux/Docker sem dependências nativas problemáticas (alinhado à ADR 0002), e a licença gratuita (Community) cobre o caso de uso, que é não-comercial ou com receita anual abaixo do limite da licença.

Apenas confirmados: a portaria, no momento em que alguém chega, precisa saber quem vai comparecer, não o histórico completo de respostas.

Ordenação alfabética por nome: com a lista já restrita a confirmados, o critério de busca real na portaria é o nome da pessoa, não o grupo ou família a que ela pertence.

## Consequências

A geração do PDF depende da biblioteca QuestPDF, que precisa ser adicionada como dependência do projeto (conforme convenção do CLAUDE.md, dependências novas passam por aprovação antes de entrar no projeto).

A query de exportação filtra por status Confirmado (ADR 0008) e ordena por nome do convidado, ignorando o agrupamento por convite.

O layout exato (colunas, espaçamento, campo de check manual) é detalhe de implementação a refinar quando a página de exportação for construída, não bloqueia o design da estrutura da solution.
