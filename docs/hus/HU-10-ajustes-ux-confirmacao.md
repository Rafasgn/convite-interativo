# HU-10 — Ajustes UX de confirmação por modo

## Objetivo

Refinar a UX dos dois modos de confirmação depois do primeiro teste manual real:

1. **Modo Grupo**: adicionar indicador de status atual acima dos botões (hoje não tem feedback do que já foi respondido).
2. **Modo Individual**: tornar cada bloco (nome + botões) mais compacto e adicionar scroll interno na área de convidados quando passar de 2 pessoas — mantém pergaminho no mesmo tamanho independente do número de convidados.

## Escopo

Dentro:

- CSS + partial Razor apenas
- Status de confirmação do Grupo exibido acima dos botões
- Botões do Individual com ~70% do tamanho dos do Grupo
- Área de convidados no Individual com `overflow-y: auto`, altura máxima calibrada pra ~2 convidados visíveis
- Sombra sutil na base do container de scroll indicando "tem mais abaixo"

Fora:

- Mudança em modelo, service, testes
- Paginação
- Botão "editar resposta" com toggle (a edição já é possível clicando de novo)
- Toast de confirmação pós-POST (débito de UX registrado, pode virar HU futura)

## Modo Grupo — status acima dos botões

Como no Grupo o status é aplicado a todos os `Convidado` do `Convite` batch, basta pegar o status do primeiro convidado (todos têm o mesmo).

Helper novo em `IndexModel`:

```csharp
public static string? StatusGrupoExibicao(List<Convidado> convidados)
{
    if (convidados.Count == 0)
    {
        return null;
    }

    return convidados[0].Status switch
    {
        StatusConfirmacao.Confirmado => "✓ Presença confirmada — pode editar abaixo",
        StatusConfirmacao.NaoVai => "✗ Marcado como ausente — pode editar abaixo",
        _ => null
    };
}
```

Na partial, dentro do bloco `else` (Grupo), antes do `<div class="pp-botoes-grupo">`:

```razor
@{
    var statusGrupo = ConviteInterativo.Web.Pages.Convite.IndexModel.StatusGrupoExibicao(Model.Convidados);
}
@if (statusGrupo is not null)
{
    <p class="pp-status-grupo">@statusGrupo</p>
}
```

CSS novo em `animacao.css`:

```css
.pp-status-grupo {
    font-family: Georgia, 'Times New Roman', serif;
    font-size: 0.85rem;
    font-style: italic;
    color: #4a2f18;
    margin: 0 0 0.5rem;
    text-align: center;
}
```

## Modo Individual — compactação + scroll

### Nova estrutura na partial

Encapsula os blocos de convidados num container próprio que vai ser o alvo do scroll:

```razor
@if (Model.Convite.ModoConfirmacao == ConviteInterativo.Web.Data.Entities.ModoConfirmacao.Individual)
{
    <div class="pp-convidados-scroll">
        @foreach (var convidado in Model.Convidados)
        {
            <div class="pp-convidado-bloco">
                <p class="pp-convidado-nome">
                    @ConviteInterativo.Web.Pages.Convite.IndexModel.NomeExibicao(convidado, Model.Convite.Nome)
                    <span class="pp-convidado-status">(@ConviteInterativo.Web.Pages.Convite.IndexModel.StatusExibicao(convidado.Status))</span>
                </p>
                <form method="post">
                    <button type="submit" class="btn btn-individual" asp-page-handler="ConfirmarIndividual" name="convidadoId" value="@convidado.Id">Confirmar presença</button>
                    <button type="submit" class="btn btn-secundario btn-individual" asp-page-handler="RecusarIndividual" name="convidadoId" value="@convidado.Id">Não poderei comparecer</button>
                </form>
            </div>
        }
    </div>
}
```

Simplificações do padrão atual:

- Um `<form>` por convidado (não dois) — mesma técnica dos 2 buttons com `formaction` que já foi validada no Grupo
- `convidadoId` vai como `name="convidadoId" value="@convidado.Id"` no próprio `<button>` — envia junto com o submit sem precisar de `<input type="hidden">`
- Bloco de cada convidado num `<div class="pp-convidado-bloco">` pra estilizar espaçamento

### CSS

```css
/* Container scrollável dos convidados no modo Individual */
.pp-convidados-scroll {
    max-height: 12rem;
    overflow-y: auto;
    width: 100%;
    padding: 0 0.5rem;
    box-sizing: border-box;
    scrollbar-width: thin;
    scrollbar-color: rgba(74, 47, 24, 0.4) transparent;

    /* Sombra sutil na base indicando "tem mais" */
    mask-image: linear-gradient(to bottom, black calc(100% - 1.5rem), transparent 100%);
    -webkit-mask-image: linear-gradient(to bottom, black calc(100% - 1.5rem), transparent 100%);
}

.pp-convidados-scroll::-webkit-scrollbar {
    width: 6px;
}

.pp-convidados-scroll::-webkit-scrollbar-thumb {
    background: rgba(74, 47, 24, 0.4);
    border-radius: 3px;
}

/* Quando não passa de 2 convidados, sem sombra nem scroll visual */
.pp-convidados-scroll.pp-sem-scroll {
    mask-image: none;
    -webkit-mask-image: none;
}

.pp-convidado-bloco {
    margin-bottom: 0.75rem;
    display: flex;
    flex-direction: column;
    align-items: center;
}

.pp-convidado-bloco:last-child {
    margin-bottom: 0;
}

.pp-convidado-nome {
    font-family: Georgia, 'Times New Roman', serif;
    font-size: 0.85rem;
    font-weight: 600;
    color: #4a2f18;
    margin: 0 0 0.25rem;
    text-align: center;
}

.pp-convidado-status {
    font-weight: 400;
    font-style: italic;
    font-size: 0.75rem;
}

/* Botões menores no modo Individual — 70% do tamanho do Grupo */
.pp-confirmacao .btn.btn-individual {
    width: 155px;
    aspect-ratio: 3.5 / 1;
    margin: -7px 0;
}
```

### Detecção "sem scroll necessário"

Pra sombra não aparecer quando tem 1-2 convidados (sem overflow), o JS pode adicionar a classe `.pp-sem-scroll` depois de renderizar:

```javascript
// No animacao.js, dentro de mostrarTudoAgora() ou num setTimeout inicial:
var scrollContainer = document.querySelector('.pp-convidados-scroll');
if (scrollContainer && scrollContainer.scrollHeight <= scrollContainer.clientHeight) {
    scrollContainer.classList.add('pp-sem-scroll');
}
```

Alternativa mais simples: sempre aplicar a máscara. Com 1-2 convidados a sombra vai aparecer na base mas mal-se-notará. Se ficar feio, ativa o JS.

**Recomendação**: começar sem o JS, ver como fica visualmente. Adiciona só se a sombra vazia incomodar.

## Arquivos afetados

1. `src/ConviteInterativo.Web/Pages/Convite/Index.cshtml.cs` — adiciona `StatusGrupoExibicao` estático
2. `src/ConviteInterativo.Web/Pages/Convite/_TemaPequenoPrincipe.cshtml` — reestrutura o bloco Individual + adiciona status no Grupo
3. `themes/pequeno-principe/animacao.css` — regras novas

## Critérios de aceite

Manuais:

- **Grupo com resposta pendente**: nenhum status aparece acima dos botões, comportamento atual
- **Grupo confirmado**: "✓ Presença confirmada — pode editar abaixo" acima dos botões
- **Grupo recusado**: "✗ Marcado como ausente — pode editar abaixo" acima dos botões
- **Individual com 1-2 convidados**: sem scroll visível, layout idêntico ao atual, botões ~70% menores
- **Individual com 3+ convidados**: scroll interno funciona, sombra na base indica que tem mais, primeiros 2 visíveis por padrão
- **Individual — cada botão continua enviando o `convidadoId` correto**: PRG funcionando, status atualiza após clicar
- Mobile (iPhone SE): tudo ainda cabe

## Riscos e mitigações

| Risco | Mitigação |
|---|---|
| `name="convidadoId" value="X"` no botão em vez de input hidden pode não funcionar em navegadores antigos | Testar no Chrome/Firefox/Safari mobile. Se falhar, volta pro `<input type="hidden">` |
| Scroll interno bloqueia scroll da página no mobile (touch conflict) | Testar. Se conflitar, adiciona `overscroll-behavior: contain` |
| Sombra CSS `mask-image` não suportada em navegador antigo | Fallback pra `linear-gradient` como background — mas Chrome/Safari/Firefox modernos suportam há anos |

## Próximas HUs depois desta

- **HU-06** — Exportação PDF portaria
- **HU-07** — Deploy (crítico, 29 dias)
- **HU polimento** — toast pós-confirmação, tela de confirmação sem re-animar
