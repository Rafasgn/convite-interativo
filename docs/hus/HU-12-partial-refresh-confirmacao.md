# HU-12 — Partial refresh do bloco de confirmação

## Objetivo

Eliminar o reload completo da página `/c/{token}` quando o convidado clica em Confirmar ou Recusar. Hoje, cada resposta dispara PRG → recarga da animação inteira (~10s até voltar ao pergaminho). O comportamento ideal é que apenas a área `#pp-confirmacao` seja atualizada, com atualização visual instantânea, sem re-animar nada.

Padrão da solução: **partial view refresh via AJAX** — servidor devolve HTML só do bloco, JS substitui no DOM. Reaproveita 100% do CSS/HTML existente. Handlers continuam retornando view (não viram API JSON), então testes existentes continuam válidos.

## Escopo

Dentro:

- Extração do conteúdo de `#pp-confirmacao` (status Grupo + botões Grupo + lista de convidados Individual com scroll) para uma partial view Razor dedicada
- Handlers `OnPost*Async` retornam `Partial(...)` quando request tem header `X-Requested-With: XMLHttpRequest`; senão continuam com `RedirectToPage` (progressive enhancement — funciona sem JS)
- Script novo em `themes/pequeno-principe/animacao.js` (ou arquivo separado) que intercepta submit dos forms, dispara fetch, substitui o DOM
- Estado visual de "loading" no botão clicado durante o request
- Antiforgery propagado via header `RequestVerificationToken`
- Testes: adaptar handlers pra retornar Partial no cenário AJAX + mantê-los OK no cenário sem JS

Fora:

- Mudança no fluxo de email (fire-and-forget continua igual)
- Toast de feedback além do que já vem do partial atualizado
- WebSocket ou SignalR
- Otimização de payload (partial vai HTML puro, ~2-3 KB)

## Fluxo alvo

**Sem JS (fallback progressive enhancement)**:

1. Convidado clica Confirmar → form submit normal
2. Handler grava + retorna `RedirectToPage(new { token })`
3. Página recarrega — comportamento atual, ok se o convidado tem JS bloqueado

**Com JS (padrão)**:

1. Convidado clica Confirmar
2. JS captura o `submit` do form, previne default
3. JS marca botão como `disabled` + adiciona classe `.pp-btn-loading`
4. JS faz `fetch(action, { method: 'POST', body: formData, headers: { 'X-Requested-With': 'XMLHttpRequest', 'RequestVerificationToken': token } })`
5. Servidor grava + dispara notificação → retorna `Partial("_ConfirmacaoConteudo", Model)`
6. JS pega o HTML da resposta, substitui `#pp-confirmacao.innerHTML`
7. Botão volta ao estado normal (via HTML novo do partial)
8. Sem reload, sem animação, sem flash

## Arquivos afetados

### Novo: `Pages/Convite/_ConfirmacaoConteudo.cshtml`

Extrai o bloco condicional Individual/Grupo do `_TemaPequenoPrincipe.cshtml`:

```razor
@model ConviteInterativo.Web.Pages.Convite.IndexModel

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
                <form method="post" data-pp-form>
                    <button type="submit" class="btn btn-individual"
                            asp-page-handler="ConfirmarIndividual"
                            name="convidadoId" value="@convidado.Id">Confirmar presença</button>
                    <button type="submit" class="btn btn-secundario btn-individual"
                            asp-page-handler="RecusarIndividual"
                            name="convidadoId" value="@convidado.Id">Não poderei comparecer</button>
                </form>
            </div>
        }
    </div>
}
else
{
    var statusGrupo = ConviteInterativo.Web.Pages.Convite.IndexModel.StatusGrupoExibicao(Model.Convidados);
    if (statusGrupo is not null)
    {
        <p class="pp-status-grupo">@statusGrupo</p>
    }
    <form method="post" data-pp-form>
        <button type="submit" class="btn" asp-page-handler="ConfirmarGrupo">Confirmar presença</button>
        <button type="submit" class="btn btn-secundario" asp-page-handler="RecusarGrupo">Não poderei comparecer</button>
    </form>
}
```

Mudança em relação ao atual: adiciona `data-pp-form` como marcador — o JS usa esse atributo pra identificar quais forms interceptar.

### `_TemaPequenoPrincipe.cshtml`

Substitui o bloco Individual+Grupo (linhas ~15-46 hoje) por:

```razor
<partial name="_ConfirmacaoConteudo" model="Model" />
```

Fica só isso no lugar dos ~30 linhas atuais.

### `Pages/Convite/Index.cshtml.cs` — handlers

Adiciona helper privado:

```csharp
private bool EhRequisicaoAjax() =>
    Request.Headers["X-Requested-With"] == "XMLHttpRequest";

private async Task<IActionResult> RetornarPartialOuRedirectAsync(string token)
{
    if (!EhRequisicaoAjax())
    {
        return RedirectToPage(new { token });
    }

    // Recarrega o dto atualizado pra popular o Model do partial
    var atualizado = await service.CarregarPorTokenAsync(token);
    if (atualizado is null)
    {
        return NotFound();
    }

    CarregarDto(atualizado);
    return Partial("_ConfirmacaoConteudo", this);
}
```

Nos 4 handlers `OnPost*Async`, substitui a chamada final `return RedirectToPage(new { token })` por `return await RetornarPartialOuRedirectAsync(token)`.

Handlers de notificação de email continuam disparando antes disso, como já estão.

### Novo: `themes/pequeno-principe/confirmacao-ajax.js`

Arquivo separado — mantém `animacao.js` limpo, escopo bem definido:

```javascript
(function () {
  'use strict';

  var container = document.getElementById('pp-confirmacao');
  if (!container) return;

  // Delegação de evento — pega submit em qualquer form dentro do container,
  // mesmo depois que o partial for substituído
  container.addEventListener('submit', function (e) {
    var form = e.target.closest('form[data-pp-form]');
    if (!form) return;

    e.preventDefault();

    var submitButton = form.querySelector('button[type="submit"]:focus') ||
                       document.activeElement.closest('button[type="submit"]') ||
                       form.querySelector('button[type="submit"]');

    // Se veio de um button específico com name/value (padrão modo Individual),
    // precisa incluir esse par no FormData
    var formData = new FormData(form);
    if (submitButton && submitButton.name && submitButton.value) {
      formData.append(submitButton.name, submitButton.value);
    }

    // Handler vem do formaction (asp-page-handler gera formaction no button)
    var action = submitButton && submitButton.formAction ? submitButton.formAction : form.action;

    // Loading state
    var todosBotoes = container.querySelectorAll('button[type="submit"]');
    todosBotoes.forEach(function (b) { b.disabled = true; });
    if (submitButton) submitButton.classList.add('pp-btn-loading');

    // Antiforgery — Razor Pages já injeta __RequestVerificationToken como input hidden no form
    var token = formData.get('__RequestVerificationToken');

    fetch(action, {
      method: 'POST',
      body: formData,
      headers: {
        'X-Requested-With': 'XMLHttpRequest',
        'RequestVerificationToken': token || ''
      },
      credentials: 'same-origin'
    })
      .then(function (resp) {
        if (!resp.ok) throw new Error('Resposta não OK: ' + resp.status);
        return resp.text();
      })
      .then(function (html) {
        container.innerHTML = html;
      })
      .catch(function (err) {
        console.error('Falha no submit AJAX, fazendo fallback pra submit normal', err);
        // Fallback: submit normal (recarrega página inteira)
        todosBotoes.forEach(function (b) { b.disabled = false; });
        if (submitButton) submitButton.classList.remove('pp-btn-loading');
        form.submit();
      });
  });
})();
```

Inclusão em `_TemaPequenoPrincipe.cshtml`:

```html
<script src="/themes/pequeno-principe/confirmacao-ajax.js" defer></script>
```

Colocar depois do `<script src=".../animacao.js" defer>`.

### `themes/pequeno-principe/animacao.css` — estado loading

Adiciona:

```css
.pp-btn-loading {
  opacity: 0.6;
  cursor: wait;
}

.pp-btn-loading::after {
  content: ' ...';
}
```

Sutil. Sem spinner porque o request é rápido (~200ms local, ~500ms produção).

## Testes

Adaptação dos testes existentes de handlers em `IndexModel` (se existirem) ou criação de novos em arquivo dedicado `IndexModelTests.cs`:

- `OnPostConfirmarGrupoAsync_semAjax_retornaRedirect` — comportamento atual mantido
- `OnPostConfirmarGrupoAsync_comAjax_retornaPartial` — header `X-Requested-With` presente
- Análogo pros outros 3 handlers

Se `IndexModelTests.cs` não existir e testes de handler nunca foram feitos (memória: 30 testes atuais, todos no `ConvitePublicoServiceTests.cs`, `ConviteServiceTests.cs`, `ConvidadoServiceTests.cs`, `PdfConfirmadosServiceTests.cs`, `NotificacaoServiceTests.cs`), **pula essa criação**. Custo vs benefício — o fluxo AJAX é testado manualmente no navegador, que é onde o valor real está.

Meta prática: **30 testes atuais continuam passando**, sem quebra em nenhum handler. Zero teste novo.

## Critérios de aceite

Manuais:

- Convite Grupo: clica Confirmar → botão fica meio-transparente por ~200ms → status "✓ Presença confirmada" aparece acima dos botões, animação NÃO reroda, cena atrás continua intacta
- Convite Individual com 5 convidados: rola pra baixo, clica Confirmar do 3º convidado → status dele muda pra (Confirmado), scroll permanece na mesma posição, botão de outros convidados intacto
- Sem JS (DevTools → Settings → Debugger → Disable JavaScript): comportamento antigo continua funcionando (reload + re-animação)
- Email de notificação chega normalmente (sem quebrar HU-11)
- Recusar funciona igual Confirmar
- Duas cliques rápidos no mesmo botão: segundo clique bloqueado (botão disabled durante request)
- Erro de rede simulado (DevTools → Network → Offline, tenta clicar): fallback pra submit normal (reload) — não trava a página

## Riscos e mitigações

| Risco | Mitigação |
|---|---|
| Antiforgery falha no header (Razor Pages exige `__RequestVerificationToken`) | Testar. Se falhar, adicionar `[IgnoreAntiforgeryToken]` no handler seria a saída errada; a certa é confirmar que o token vai no header via nome exato `RequestVerificationToken` (padrão .NET) |
| Handler retorna Partial mas Layout=null da HU-05a interfere | Partial não usa layout, então não afeta. Testar |
| Botão com `name="convidadoId" value="X"` (Individual) não é incluído em FormData sem help | JS lê explicitamente e adiciona antes do fetch — código já cobre |
| `activeElement` no submit não bate com o button clicado em navegador antigo | Fallback pra primeiro button do form — comportamento sub-ótimo mas funcional |
| Cache de browser guardando resposta AJAX antiga | Adicionar `Cache-Control: no-store` no handler ou header `Pragma: no-cache` na resposta do Partial |
| Formulário HTML5 com validação falha silenciosa no AJAX | Não tem validação no lado convidado — só botão. N/A |
| Mudança na partial extraída deixa a estrutura duplicada no `_TemaPequenoPrincipe.cshtml` por engano | Confirmar que o bloco original foi substituído, não duplicado |

## Sequência de implementação

1. Criar `_ConfirmacaoConteudo.cshtml` com o conteúdo extraído
2. Substituir o bloco no `_TemaPequenoPrincipe.cshtml` por `<partial name="_ConfirmacaoConteudo" model="Model" />`
3. Adicionar helper `EhRequisicaoAjax` + `RetornarPartialOuRedirectAsync` no `IndexModel`
4. Trocar `RedirectToPage` por `await RetornarPartialOuRedirectAsync(token)` nos 4 handlers
5. Criar `themes/pequeno-principe/confirmacao-ajax.js`
6. Incluir o script no `_TemaPequenoPrincipe.cshtml`
7. Adicionar `.pp-btn-loading` no `animacao.css`
8. Build + rodar testes existentes (todos devem continuar passando)
9. Teste manual completo dos critérios de aceite

## Próximas HUs depois desta

- **HU-07** — Deploy (crítico, 28 dias). Depois da HU-12 fecha o produto conceitualmente
- **HU polimento** — coisas menores acumuladas (débito :hover botões, breadcrumb admin, toast global)
