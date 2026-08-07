# 0012 — Teaser estático por tema como og:image para preview no WhatsApp

**Data:** 2026-08-06
**Status:** Aceita (revisa a decisão original de 2026-08-06)

## Contexto

Quando um link é colado no WhatsApp, o app busca meta tags como og:image, og:title e og:description da página para montar o preview no chat, antes mesmo de o destinatário clicar. Esse comportamento não estava coberto na visão inicial. A primeira versão desta ADR assumia uma imagem gerada por convite (ver ADR 0011, superseded); o modelo real é mais simples.

**Nota:** o caminho físico de teaser.png (themes/<slug>/assets/ na raiz do repo, não wwwroot/) foi decidido durante a revisão do plano da solution .NET, não estava explícito na versão original deste ADR. Este documento já foi corrigido para refletir essa decisão.

## Decisão

Existe apenas uma imagem estática por tema, chamada teaser.png: uma arte "limpa" (sem data, local, horário ou nomes de convidados), com uma mensagem genérica do tipo "Rafael, Ana Carolina e Zayan têm um convite muito especial pra você. Clique no link abaixo pra abrir". Essa imagem é a mesma para todos os convidados do evento.

O arquivo fica em themes/<slug>/assets/teaser.png, na raiz do repo (mesmo local dos demais assets do tema, ADR 0010) — não em wwwroot/. Em runtime, essa pasta é servida via PhysicalFileProvider mapeado em Program.cs no path /themes, então a URL pública é /themes/<slug>/assets/teaser.png, usada como og:image da página pública /Convite/{token}. Não há geração nem manipulação de imagem em runtime: quando o admin clica em "gerar link", o sistema só cria o token e mostra a URL (ADR 0005); a imagem não entra nesse fluxo.

O envio da imagem-teaser junto com o link no WhatsApp é feito manualmente pelo usuário. O WhatsApp reconhece o link como clicável e monta o preview a partir do og:image da página, que é o mesmo teaser.png.

og:title e og:description ficam a decidir na implementação: podem ser genéricos por tema (ex.: "Você tem um convite especial") ou personalizados por convite — não é uma decisão de estrutura, é detalhe de conteúdo.

## Motivação

Sem meta tags corretas, o link aparece sem preview no WhatsApp, quebrando o efeito de imagem bonita mais link descrito na visão do produto.

Uma imagem estática por tema resolve o preview do WhatsApp sem precisar de composição de imagem em runtime, sem dependência de biblioteca de imagem (SkiaSharp, descartado — ver ADR 0011) e sem necessidade de armazenamento de imagens geradas.

Como a mensagem do teaser é genérica (não cita nomes nem dados do evento), a mesma imagem serve para todos os convidados de todos os convites do evento, sem precisar de uma imagem por convite.

## Consequências

teaser.png é um asset estático do tema, versionado junto com o resto de themes/<slug>/assets/, igual a qualquer outra arte do tema (ADR 0010) — não precisa de pasta separada nem de volume persistente, ao contrário do banco SQLite (ADR 0002).

O mecanismo de serving é um PhysicalFileProvider configurado em Program.cs, apontando para a pasta themes/ da raiz do repo e mapeado no request path /themes — sem cópia nem duplicação em wwwroot/.

A página /Convite/{token} precisa renderizar as meta tags Open Graph apontando para a URL pública e absoluta de /themes/<slug>/assets/teaser.png do tema daquele evento/convite.

O fluxo de "gerar link" no admin fica mais simples: só cria o token e exibe a URL, sem nenhum passo de imagem.

Não há mais necessidade de uma ADR sobre pasta/volume persistente para imagens geradas (o que estava cogitado como um possível ADR 0014) — não existe imagem gerada no sistema.
