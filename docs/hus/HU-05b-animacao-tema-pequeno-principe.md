# HU-05b — Animação do tema Pequeno Príncipe

## Objetivo

Substituir a view sóbria da HU-05a (`Pages/Convite/Index.cshtml`) por uma versão com o tema visual completo do Pequeno Príncipe: animação de entrada, sprites empilhados, efeito de digitação nos nomes dos convidados, e apresentação dos dados do evento.

Mantém 100% da lógica da HU-05a — os 5 handlers (`OnPost*Async`), o service `ConvitePublicoService`, os testes existentes e o comportamento de confirmação nos dois modos (Grupo / Individual) continuam intocados. HU-05b é substituição de **camada visual**.

## Escopo

Dentro:

- Animação de entrada única (~12s) com sprites empilhados
- Efeito de digitação revelando os nomes dos convidados
- Apresentação dos dados do evento (data, endereço, link do mapa) após a animação
- Botão "pular animação" pra quem já viu
- Respeitar `prefers-reduced-motion` (pula direto pro estado final)
- Layout responsivo (desktop e mobile)

Fora:

- Áudio (ADR 0001)
- Motor configurável via banco (ADR 0009 — animação fixa por tema)
- Novos endpoints, novos handlers, mudança em `ConvitePublicoService`
- Alterações no domínio (Convite, Convidado, Evento)

## Sprites usados

Referenciados de `/assets/miniaturas_separadas/` via static files (PhysicalFileProvider já configurado no `Program.cs`):

| Sprite | Papel na cena |
|---|---|
| `16_estrelas_particulas.png` | Fundo estrelado |
| `18_nuvens_brancas.png` | Nuvens flutuantes |
| `24_esfera_estrelada.png` | Halo estrelado central |
| `09_asteroide_rosa.png` | Asteroide B-612 (onde o Príncipe fica sentado) |
| `03_principe_sentado.png` | Príncipe no asteroide |
| `21_rastro_dourado.png` | Estrela cadente (cruza a tela) |
| `25_lua_crescente.png` | Lua no canto |
| `06_raposa_sentada.png` | Raposa |
| `17_rosa_redoma.png` | Rosa na redoma |
| `26_pergaminho_grande.png` | Pergaminho onde os nomes aparecem |
| `32_icones.png` | Sprite sheet dos ícones dos dados do evento (data, local, mapa) |
| `30_botao_confirmar.png` | Estado visual do botão confirmar |

Sobras que ficam de reserva (não usadas nesta HU): 02, 05, 07, 08, 11-15, 22, 23, 27, 33, 34.

**Ponto aberto na implementação**: `32_icones.png` é sprite sheet. Duas opções pro Claude Code decidir:

1. Usar como `background-image` com `background-position` recortando cada ícone via CSS.
2. Cortar em arquivos individuais (`icone-data.png`, `icone-local.png`, etc).

Opção 1 evita duplicação e mantém 1 arquivo só; opção 2 é mais simples de manter. Preferência: opção 1.

## Estrutura de arquivos

```
themes/pequeno-principe/
├── animacao.html      # markup do tema (incluído inline via Razor)
├── animacao.css       # keyframes, layout, camadas, responsividade
├── animacao.js        # timeline, digitação, skip, prefers-reduced-motion
└── teaser.png         # já existe (og:image, não muda)
```

Os stubs criados no scaffold (06/ago) ganham conteúdo real.

## Camadas (z-index)

Todos os elementos ficam absolutamente posicionados dentro de um container `#cena` de tamanho fixo com `position: relative`. `z-index` fixo por camada:

| z-index | Camada | Sprites |
|---|---|---|
| 0 | Fundo estrelado | `16` |
| 10 | Nuvens | `18` |
| 15 | Halo estrelado | `24` |
| 20 | Corpos celestes | `25` (lua) |
| 25 | Estrela cadente | `21` (one-shot, some depois) |
| 30 | Asteroide + Príncipe | `09` + `03` |
| 35 | Raposa | `06` |
| 40 | Rosa na redoma | `17` |
| 50 | Pergaminho | `26` |
| 55 | Texto sobre o pergaminho | (nomes, dados do evento) |
| 60 | Ícones dos dados | `32` |
| 70 | Botões de confirmação | (herdados da HU-05a, estilizados com `30`) |
| 90 | Skip button | (canto superior direito) |

## Timeline

Marcos em milissegundos, do carregamento do DOM:

| t (ms) | Evento |
|---|---|
| 0 | Fundo estrelado fade-in (`16`) |
| 500 | Nuvens flutuando (`18`, animação contínua) + halo (`24`) |
| 1500 | Lua entra deslizando do canto (`25`) |
| 2500 | Asteroide + Príncipe entram por baixo (`09` + `03`) |
| 4000 | Estrela cadente cruza a tela em diagonal (`21`, ~1s de duração) |
| 5500 | Raposa aparece ao lado do Príncipe (`06`) |
| 7000 | Rosa na redoma desce do topo (`17`) |
| 8500 | Pergaminho desenrola no centro (`26`) |
| 9500 | Digitação dos nomes começa (100ms por caractere) |
| ~11000 | Dados do evento aparecem (data, endereço, mapa) com ícones (`32`) |
| ~12000 | Botão(ões) de confirmação ficam visíveis e clicáveis |

Duração total: ~12s até o botão. Skip pula direto pra t=12000.

## Sequenciamento (animacao.js)

Duas alternativas:

- **Cadeia de `setTimeout`** com os marcos acima, adicionando classes CSS que disparam keyframes de entrada.
- **Web Animations API** (`element.animate(...)`) com Promise por elemento e `await`.

Preferência: `setTimeout` — mais legível pra timeline linear, sem dependência nova.

Efeito de digitação: JS puro, `setInterval` de 100ms revelando um caractere por vez. Cursor piscando (`::after` com animation) enquanto digita, some no final.

## Integração com a Razor page

`Pages/Convite/Index.cshtml` da HU-05a:

- Layout já é `null` (via `_ViewStart.cshtml` da HU-05a)
- Injeta os dados do DTO (`ConvitePublicoDto`) via `<script>window.dadosConvite = @Json.Serialize(new { ... })</script>` no head, antes de carregar `animacao.js`
- Inclui `<link rel="stylesheet" href="/themes/pequeno-principe/animacao.css">` e `<script src="/themes/pequeno-principe/animacao.js" defer></script>`
- O markup do `animacao.html` vira uma partial `_TemaPequenoPrincipe.cshtml` incluída na página (ou é inline direto — decisão do Claude Code baseada no tamanho)

Os handlers de POST (`OnPostConfirmarGrupoAsync`, `OnPostConfirmarIndividualAsync`, etc) continuam nos POSTs padrão. Os botões renderizados pela Razor recebem classes CSS do tema pra aparência.

## Dados dinâmicos

O tema não sabe de onde vêm os dados — só consome `window.dadosConvite`:

```js
{
  nomes: ["Ana", "João"],          // pra digitação
  evento: {
    dataHora: "2026-10-24T19:00",
    endereco: "...",
    linkMapa: "https://..."
  },
  modo: "Grupo" | "Individual",
  convidados: [
    { id: 1, nome: "Ana Silva", status: "SemResposta" },
    ...
  ]
}
```

Regra de display "Nome (NomeConvite)" quando `Sobrenome` vazio (decisão de 11/ago) continua sendo aplicada server-side no DTO — o tema só consome o `nome` já formatado.

## Acessibilidade

- `@media (prefers-reduced-motion: reduce)`: todas as animações têm duração 0, os elementos aparecem já no estado final. Digitação também vira revelação instantânea.
- Skip button: `<button>` com `aria-label="Pular animação"`, visível desde t=0.
- Todos os sprites como `<img>` com `alt=""` (decorativos) — o conteúdo textual (nomes, dados) fica em elementos semânticos separados.
- Contraste: garantir que texto sobre o pergaminho tenha contraste WCAG AA (`26_pergaminho_grande` tem fundo bege, texto marrom escuro).

## Responsividade

Container `#cena` com `aspect-ratio: 9/16` (mobile-first, retrato).

Breakpoints:

- Mobile (< 768px): cena vertical, elementos empilhados verticalmente.
- Desktop (>= 768px): cena centralizada com largura máxima ~480px (mantém proporção mobile — é convite, não site institucional).

Sprites usam `object-fit: contain` pra não distorcer.

## Testes / critérios de aceite

Automatizados (poucos, animação é difícil de testar):

- Rota `/c/{token}` continua renderizando (herdado da HU-05a).
- HTML da resposta contém os elementos-chave do tema (seletores CSS: `#cena`, `.sprite`, `#pergaminho`).
- `<script>window.dadosConvite = ...</script>` presente com os dados esperados.

Manuais (Rafael valida no navegador):

- Animação roda de ponta a ponta sem travamento (Chrome, Firefox, Safari iOS).
- Nomes dos convidados aparecem com digitação.
- Modo Grupo mostra 1 botão de confirmação; modo Individual mostra 1 por convidado.
- Botão confirmar POST bate no handler correto (regressão da HU-05a).
- `prefers-reduced-motion: reduce` (config do OS ou DevTools) pula direto pro estado final.
- Skip button pula pra t=12000.
- Mobile (iPhone e Android) renderiza sem quebrar layout.
- Já respondido: estado atual visível, permite reeditar (herdado da HU-05a).

## Riscos e mitigações

| Risco | Mitigação |
|---|---|
| Sprites com resolução alta pesarem o carregamento | Pré-otimização (compressão PNG, WebP opcional); `loading="eager"` só nos sprites da primeira cena |
| Timeline com `setTimeout` dessincronizar em abas em background | `document.visibilityState`: pausa a timeline se `hidden` |
| WhatsApp abrir link em WebView interno com CSS quebrado | Testar no WhatsApp Android/iOS antes de fechar HU |
| Cursor de digitação piscando aparecer em teclado virtual mobile | Verificar que o cursor é `::after` decorativo, não input real |
| Sprite sheet `32_icones` não renderizar direito em algum navegador | Fallback: se a decisão for opção 1 (background-position), documentar as coordenadas do sprite sheet; se opção 2 falhar em algum ponto, cortar em arquivos separados como plano B |

## Próximas HUs depois desta

- **HU-06** — Exportação PDF portaria (QuestPDF)
- **HU-07** — Deploy (ADR 0015: ForwardedHeaders + USER non-root)

## Referências

- ADR 0009 — animação fixa por tema, HTML/CSS/JS estático
- ADR 0010 — ilustrações via GPT-image; placeholders SVG (superseded pelos sprites do Gemini/GPT-image entregues em 19/ago)
- ADR 0012 — og:image aponta pra `teaser.png` estático (não muda nesta HU)
- HU-05a — `docs/hus/HU-05a-pagina-publica.md` (se existir; senão, referência em `/areas/convite-interativo.md`)
