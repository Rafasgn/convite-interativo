# HU-09 — Ajustes visuais do pergaminho

## Objetivo

Refinar a composição visual do tema Pequeno Príncipe depois do primeiro teste manual da HU-05b/08. Escopo é puramente CSS + `animacao.html` — nada de C#, sem mudança em model, service, testes.

## Mudanças

### Sprites — remoções

- **`06_raposa_sentada.png`** — remove da animação. Peça e camada inteira do `.pp-raposa` saem do DOM (`animacao.html`) e do CSS (regras `.pp-raposa`, `.fase-raposa .pp-raposa`).
- **`17_rosa_redoma.png`** — remove da animação. A rosa continua presente na cena inicial via `09_asteroide_rosa.png` editado pelo usuário (já tem a rosa no asteroide) — a redoma isolada no topo do pergaminho fica redundante. Peça e camada inteira do `.pp-rosa` saem do DOM e do CSS (regras `.pp-rosa`, `.fase-rosa .pp-rosa`).
- **Fases obsoletas**: `fase-raposa` e `fase-rosa` viram sem efeito, mas ficam no array `FASES` do `animacao.js` — o CSS que reagia a elas simplesmente sumiu. Alternativa é remover do array também (mais limpo). Preferência: **remover do array** pra não deixar timing morto.

### Sprites — adições

- **`01_principe_em_pe.png`** — canto inferior **esquerdo** do pergaminho, sobreposto (parte do corpo fora do pergaminho, sugerindo profundidade).
- **`07_raposa_dormindo.png`** — canto inferior **direito** do pergaminho, sobreposto.

Ambos são **filhos do `.pp-pergaminho-grupo`** (não do `#cena` raiz), porque devem entrar junto com o pergaminho e sair junto (se algum dia sair). Isso simplifica: herdam a mesma transição de entrada do pergaminho (opacity + scale).

Camadas:

| z-index | Elemento |
|---|---|
| 45 | `.pp-principe-perg` (Príncipe sobreposto) |
| 45 | `.pp-raposa-perg` (Raposa dormindo sobreposta) |

`z-index: 45` fica **entre** o pergaminho (50) e os corpos celestes (20/25/30) — não. Corrigindo: precisa ficar **acima** do pergaminho (que é 50). Então `z-index: 55` ou mais. Vamos com **`z-index: 55`** — junto com `.pp-pergaminho-conteudo`, que também é 55 mas em outro sub-elemento; sem colisão porque são irmãos em containers diferentes.

Melhor ainda: como são filhos de `.pp-pergaminho-grupo` (z-index 50), qualquer `z-index` interno serve — o stacking context do pai é o que define contra o resto da cena. Basta garantir ordem no HTML pra ficarem na frente da imagem do pergaminho.

### Sprites — animação de entrada + respiração

**Entrada**: mesma fase do pergaminho (`fase-pergaminho`). Entram com:

- Príncipe (esquerda): `translateX(-30px)` → `translateX(0)`, opacity 0 → 1, 1.2s ease-out.
- Raposa (direita): `translateX(30px)` → `translateX(0)`, opacity 0 → 1, 1.2s ease-out.

Delay: 0.3s depois da entrada do pergaminho — o pergaminho aparece primeiro, aí os personagens "chegam".

**Respiração (loop sutil)**: `@keyframes pp-respirar` — escala 1.0 → 1.015 → 1.0 em 3.5s, `ease-in-out`, `infinite`. Sutil o suficiente pra não competir com o conteúdo.

Príncipe usa `transform-origin: bottom center` (respira "pra cima").
Raposa usa `transform-origin: bottom center` (respira "pra cima" também — como se o peito subisse e descesse dormindo).

Respiração começa depois que a entrada termina (1.5s de delay total). Implementação: uma classe `.pp-respirando` adicionada pelo CSS via `animation-delay` calculado, ou via JS num `setTimeout` extra. **Preferência: animation-delay puro CSS** — sem tocar em JS.

**`prefers-reduced-motion`**: já está coberto pela regra genérica `.pp-cena * { animation: none !important; transition: none !important; }` no CSS atual. Nada extra.

### Layout do conteúdo do pergaminho

Estado atual (HU-05b/08): `.pp-pergaminho-conteudo` com `inset: 12% 12%`. Conteúdo fica meio no topo, botões distantes.

Ajustes:

1. **Bloco todo desce**: mudar `inset: 12% 12%` para `inset: 18% 14% 12% 14%` (topo maior, laterais um pouco menores, base normal). Isso empurra frase + nomes + dados pra baixo, dando mais respiro no topo do pergaminho onde as estrelas decorativas ficam.

2. **Fonte dos nomes menor**: `.pp-nomes` de `1.1rem` → `0.95rem`. Duas coisas resolvidas: nomes longos (tipo "Roselande Dos Santos Gonçalves e Tereza Cristina dos santros") quebram menos, e sobra espaço vertical pros botões.

3. **Botões juntos**: hoje `.pp-confirmacao .btn { margin: 0.35rem; }` — dá gap grande entre confirmar e recusar. Mudar pra `margin: 0.15rem 0.35rem;` (mantém margem lateral, encurta vertical). Além disso, `.pp-confirmacao { margin-top: 0.5rem; }` (era 1rem) — encosta os botões no bloco de dados.

4. **Espaçamento da frase**: `.pp-frase-convite { margin: 0 0 0.5rem; }` (era `0.75rem`) — encosta um pouco mais na parte dos nomes.

## Arquivos afetados

### `themes/pequeno-principe/animacao.html`

Adições dentro de `.pp-pergaminho-grupo`, depois de `.pp-pergaminho-img` e antes de `.pp-pergaminho-conteudo`:

```html
<img class="sprite pp-principe-perg"
     src="/assets/miniaturas_separadas/01_principe_em_pe.png"
     alt="" />

<img class="sprite pp-raposa-perg"
     src="/assets/miniaturas_separadas/07_raposa_dormindo.png"
     alt="" />
```

Remoções: as duas linhas `<img class="sprite pp-raposa" ...>` e `<img class="sprite pp-rosa" ...>` que estão hoje no arquivo (fora do `.pp-pergaminho-grupo`).

### `themes/pequeno-principe/animacao.css`

**Remove** (blocos inteiros):

- `/* ===== z-index 35 — raposa ===== */` e regra `.pp-cena.fase-raposa .pp-raposa`
- `/* ===== z-index 40 — rosa na redoma ===== */` e regra `.pp-cena.fase-rosa .pp-rosa`

**Ajusta** (regras existentes):

```css
.pp-pergaminho-conteudo {
  inset: 18% 14% 12% 14%;  /* era: 12% 12% */
  /* resto igual */
}

.pp-nomes {
  font-size: 0.95rem;  /* era: 1.1rem */
  /* resto igual */
}

.pp-frase-convite {
  margin: 0 0 0.5rem;  /* era: 0 0 0.75rem */
  /* resto igual */
}

.pp-confirmacao {
  margin-top: 0.5rem;  /* era: 1rem */
  /* resto igual */
}

.pp-confirmacao .btn {
  margin: 0.15rem 0.35rem;  /* era: 0.35rem */
  /* resto igual */
}
```

**Adiciona** (blocos novos, depois do `.pp-pergaminho-img`):

```css
/* Personagens sobrepostos ao pergaminho */
.pp-principe-perg,
.pp-raposa-perg {
  position: absolute;
  bottom: -8%;
  width: 28%;
  opacity: 0;
  transform-origin: bottom center;
  transition: opacity 1.2s ease-out, transform 1.2s ease-out;
}

.pp-principe-perg {
  left: -6%;
  transform: translateX(-30px);
}

.pp-raposa-perg {
  right: -6%;
  transform: translateX(30px);
}

.pp-cena.fase-pergaminho .pp-principe-perg,
.pp-cena.fase-pergaminho .pp-raposa-perg {
  opacity: 1;
  transform: translateX(0);
  animation: pp-respirar 3.5s ease-in-out infinite;
  animation-delay: 1.5s;
}

@keyframes pp-respirar {
  0%, 100% { transform: translateX(0) scale(1); }
  50%      { transform: translateX(0) scale(1.015); }
}
```

Nota: a `animation` sobrescreve o `transform` da transition depois que ela termina — é o comportamento desejado (entra deslizando, depois começa a respirar).

### `themes/pequeno-principe/animacao.js`

Remove do array `FASES`:

```javascript
{ t: 5500, classe: 'fase-raposa' },
{ t: 7000, classe: 'fase-rosa' },
```

Ajusta os `t` das fases seguintes pra encurtar o tempo total (a timeline vai de ~12s pra ~10s, o que é bom):

| Fase | t antes | t depois |
|---|---|---|
| fase-fundo | 0 | 0 |
| fase-nuvens | 500 | 500 |
| fase-lua | 1500 | 1500 |
| fase-principe | 2500 | 2500 |
| fase-estrela-cadente | 4000 | 4000 |
| ~~fase-raposa~~ | ~~5500~~ | (removido) |
| ~~fase-rosa~~ | ~~7000~~ | (removido) |
| fase-pergaminho | 8500 | 6500 |
| fase-digitando | 9500 | 7500 |
| fase-dados | 11000 | 9000 |
| fase-confirmacao | 12000 | 10000 |

Ajusta também o timeout de safety net de 15000ms pra manter a folga de ~5s: **fica em 15000ms mesmo** — reduzir não é necessário e mantém segurança maior.

## Critérios de aceite

Manuais (Rafael valida no navegador):

- Animação inicial: Príncipe entra no asteroide, estrela cadente cruza, lua aparece — **sem** raposa acordada, **sem** rosa na redoma isolada (elas não existem mais na sequência).
- Pergaminho abre e, ~0.3s depois, Príncipe em pé aparece deslizando pelo canto inferior esquerdo, raposa dormindo pelo canto inferior direito.
- Depois da entrada, ambos ficam com respiração sutil (loop 3.5s).
- Nomes dos convidados ficam menores, cabem em duas linhas sem quebrar feio.
- Frase, nomes, dados do evento e botões ficam mais concentrados na parte inferior do pergaminho (com respiro no topo).
- Botões Confirmar e Não poderei comparecer ficam próximos (sem gap grande entre eles).
- `prefers-reduced-motion: reduce`: respiração some, entrada instantânea, tudo como o CSS atual já garante.
- Mobile (Chrome DevTools iPhone): personagens no canto do pergaminho não vazam pra fora do container `.pp-cena`.

## Riscos e mitigações

| Risco | Mitigação |
|---|---|
| Príncipe/raposa em bottom negativo (`-8%`) vazam pra fora do `.pp-cena` no mobile | Testar; se vazar, subir pra `bottom: 0` ou ajustar `overflow` do `.pp-cena` |
| Respiração de 1.5% de escala fica imperceptível em telas pequenas | Se for o caso, subir pra 2% depois |
| Remover `fase-raposa`/`fase-rosa` do array mas esquecer alguma referência CSS órfã | Confirmar com Ctrl+F no CSS final que `fase-raposa` e `fase-rosa` não aparecem |

## Próximas HUs depois desta

- **HU-06** — Exportação PDF portaria (QuestPDF)
- **HU-07** — Deploy (crítico até 20/09)
- **HU-polimento admin** — completar o débito de navegação/UX (já parcialmente resolvido)
