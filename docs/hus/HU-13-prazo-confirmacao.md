# HU-13 — Prazo de confirmação com regras dinâmicas

## Objetivo

Adicionar prazo limite pra RSVP, com regra diferente pra convite antecipado e tardio. Comunicar visualmente o status, os botões e o acesso aos dados do evento de acordo com estado + prazo. Bloquear ações depois do prazo com política diferente pra quem já confirmou positivo (mantém dados + botão de cancelar) e quem não confirmou / negou (só mensagem de encerramento).

## Regras de prazo

**Convite antecipado** = criado antes ou no 10º dia antes do evento (`Convite.DataCriacao.Date <= Evento.DataHora.Date.AddDays(-10)`).
Prazo: `Evento.DataHora.Date.AddDays(-10)` às 23:59:59.
Exemplo: evento 24/10/2026 → prazo 14/10/2026 23:59:59.

**Convite tardio** = criado depois disso (`Convite.DataCriacao.Date > Evento.DataHora.Date.AddDays(-10)`).
Prazo: `Evento.DataHora.Date.AddDays(-2)` às 23:59:59 — sempre a antevéspera do evento, independentemente da data de criação.
Exemplo: evento 24/10/2026 → prazo tardio 22/10/2026 23:59:59.

**Dia do evento**: nenhum convite (antecipado ou tardio) permite ação. Prazo tardio termina 23:59:59 de dois dias antes; antecipado termina 10 dias antes. Depois disso, tudo bloqueado.

## Regras visuais e de ação

**Sempre visível**, em qualquer estado/prazo: frase de convite + nomes dos convidados.

### Antes do prazo (por convidado no Individual, ou pelo Convite no Grupo)

| Estado | Status texto | Botões |
|---|---|---|
| Sem resposta | `Confirme se poderá comparecer até {data}` | **Confirmar** + **Não poderei comparecer** |
| Confirmado | `✓ Presença confirmada` | Só **Não poderei comparecer** |
| Recusado | `✗ Marcado como ausente` | Só **Confirmar presença** |

Dados do evento (data/hora, endereço, "Como chegar"): visíveis em qualquer estado antes do prazo.

### Depois do prazo (por convidado)

**A) Convidado Confirmado**:
- Dados do evento visíveis
- Status: `✓ Presença confirmada`
- Só **Não poderei comparecer** (permite imprevisto)

**B) Convidado sem resposta OU Recusado**:
- Dados do evento **somem**
- Botões **somem**
- Bloco único com mensagem: `Infelizmente o prazo para confirmação se esgotou. Agradecemos pela atenção.`

## Escopo

Dentro:

- Helpers estáticos em `IndexModel`:
  - `PrazoLimite(Convite convite, Evento evento)` — retorna DateTime do prazo (10 ou 2 dias)
  - `PrazoLimiteFormatado(Convite convite, Evento evento)` — string `"14 de outubro"`
  - `PrazoEncerrado(Convite convite, Evento evento)` — bool
  - `StatusExibicaoIndividual(Convidado, Convite, Evento)` — string do status
  - `PodeVerDadosDoEvento(Convidado, Convite, Evento)` — bool (Individual)
  - `PodeVerDadosDoEventoGrupo(List<Convidado>, Convite, Evento)` — bool (Grupo — se algum confirmado, mostra pra todos; ou se antes do prazo)
  - `MostrarBotaoConfirmar(Convidado, Convite, Evento)` — bool
  - `MostrarBotaoRecusar(Convidado, Convite, Evento)` — bool
  - `MostrarBlocoPrazoEncerrado(Convidado, Convite, Evento)` — bool (só pra sem resposta ou recusado pós-prazo)
- Reestruturação do `_ConfirmacaoConteudo.cshtml` pra usar os helpers
- Reestruturação do `_TemaPequenoPrincipe.cshtml` pra esconder `#pp-dados-evento` no cenário pós-prazo B
- Guards nos 4 handlers `OnPost*Async` — POST bloqueado quando ação não é permitida pelos helpers
- Regras CSS pra layout da mensagem de encerramento

Fora:

- Mudança no `ConvitePublicoService`
- Novo campo em `Convite` — usar `DataCriacao` que já existe
- Testes automatizados do prazo — cobertura via helpers estáticos, testes unitários simples
- Envio de lembrete por email antes do prazo
- Reenvio automático após prazo — o anfitrião cadastra outro Convite pelo admin (que vira convite tardio)

## Helpers em `Index.cshtml.cs`

```csharp
using System.Globalization;

// ...

private static readonly CultureInfo CulturaPtBr = new("pt-BR");

private const int DiasAntesEventoAntecipado = 10;
private const int DiasAntesEventoTardio = 2;

public static bool EhConviteTardio(Convite convite, Evento evento)
{
    var limiteAntecipado = evento.DataHora.Date.AddDays(-DiasAntesEventoAntecipado);
    return convite.DataCriacao.Date > limiteAntecipado;
}

public static DateTime PrazoLimite(Convite convite, Evento evento)
{
    var diasAntes = EhConviteTardio(convite, evento)
        ? DiasAntesEventoTardio
        : DiasAntesEventoAntecipado;
    return evento.DataHora.Date.AddDays(-diasAntes).AddDays(1).AddSeconds(-1);
    // AddDays + AddSeconds(-1) = 23:59:59 do dia N-antes
}

public static string PrazoLimiteFormatado(Convite convite, Evento evento) =>
    PrazoLimite(convite, evento).ToString("dd 'de' MMMM", CulturaPtBr);

public static bool PrazoEncerrado(Convite convite, Evento evento) =>
    DateTime.UtcNow > PrazoLimite(convite, evento);

public static string StatusExibicaoIndividual(Convidado convidado, Convite convite, Evento evento)
{
    var estourado = PrazoEncerrado(convite, evento);
    return convidado.Status switch
    {
        StatusConfirmacao.Confirmado => "✓ Presença confirmada",
        StatusConfirmacao.NaoVai when !estourado => "✗ Marcado como ausente",
        StatusConfirmacao.NaoVai => "", // pós-prazo cai no bloco de encerramento, sem status
        _ when !estourado => $"Confirme se poderá comparecer até {PrazoLimiteFormatado(convite, evento)}",
        _ => "", // pós-prazo cai no bloco de encerramento
    };
}

public static bool MostrarBotaoConfirmar(Convidado convidado, Convite convite, Evento evento)
{
    if (PrazoEncerrado(convite, evento)) return false;
    // Antes do prazo: mostra confirmar se sem resposta ou já recusado (deixa mudar de ideia)
    return convidado.Status != StatusConfirmacao.Confirmado;
}

public static bool MostrarBotaoRecusar(Convidado convidado, Convite convite, Evento evento)
{
    if (PrazoEncerrado(convite, evento))
    {
        // Pós-prazo: só Confirmado ainda pode cancelar (imprevisto)
        return convidado.Status == StatusConfirmacao.Confirmado;
    }
    // Antes do prazo: mostra recusar se sem resposta ou já confirmado
    return convidado.Status != StatusConfirmacao.NaoVai;
}

public static bool MostrarDadosEvento(Convidado convidado, Convite convite, Evento evento)
{
    if (!PrazoEncerrado(convite, evento)) return true;
    return convidado.Status == StatusConfirmacao.Confirmado;
}

public static bool MostrarBlocoPrazoEncerrado(Convidado convidado, Convite convite, Evento evento)
{
    if (!PrazoEncerrado(convite, evento)) return false;
    return convidado.Status != StatusConfirmacao.Confirmado;
}

// Grupo: usa o primeiro convidado como referência (todos têm mesmo status no Grupo)
public static string StatusGrupoExibicao(List<Convidado> convidados, Convite convite, Evento evento) =>
    convidados.Count > 0
        ? StatusExibicaoIndividual(convidados[0], convite, evento)
        : "";

public static bool MostrarDadosEventoGrupo(List<Convidado> convidados, Convite convite, Evento evento) =>
    convidados.Count > 0 && MostrarDadosEvento(convidados[0], convite, evento);

public static bool MostrarBlocoPrazoEncerradoGrupo(List<Convidado> convidados, Convite convite, Evento evento) =>
    convidados.Count > 0 && MostrarBlocoPrazoEncerrado(convidados[0], convite, evento);
```

## Partial `_ConfirmacaoConteudo.cshtml`

Reestrutura os dois blocos:

### Individual

```razor
<div class="pp-convidados-scroll">
    @foreach (var convidado in Model.Convidados)
    {
        var mostrarBlocoEnc = ConviteInterativo.Web.Pages.Convite.IndexModel.MostrarBlocoPrazoEncerrado(convidado, Model.Convite, Model.Evento);
        var mostrarConfirmar = ConviteInterativo.Web.Pages.Convite.IndexModel.MostrarBotaoConfirmar(convidado, Model.Convite, Model.Evento);
        var mostrarRecusar = ConviteInterativo.Web.Pages.Convite.IndexModel.MostrarBotaoRecusar(convidado, Model.Convite, Model.Evento);
        var statusTexto = ConviteInterativo.Web.Pages.Convite.IndexModel.StatusExibicaoIndividual(convidado, Model.Convite, Model.Evento);

        <div class="pp-convidado-bloco">
            <p class="pp-convidado-nome">
                @ConviteInterativo.Web.Pages.Convite.IndexModel.NomeExibicao(convidado, Model.Convite.Nome)
            </p>

            @if (mostrarBlocoEnc)
            {
                <p class="pp-prazo-encerrado">Infelizmente o prazo para confirmação se esgotou. Agradecemos pela atenção.</p>
            }
            else
            {
                if (!string.IsNullOrEmpty(statusTexto))
                {
                    <p class="pp-status-inline">@statusTexto</p>
                }
                if (mostrarConfirmar || mostrarRecusar)
                {
                    <form method="post" data-pp-form>
                        @if (mostrarConfirmar)
                        {
                            <button type="submit" class="btn btn-individual"
                                    asp-page-handler="ConfirmarIndividual"
                                    name="convidadoId" value="@convidado.Id">Confirmar presença</button>
                        }
                        @if (mostrarRecusar)
                        {
                            <button type="submit" class="btn btn-secundario btn-individual"
                                    asp-page-handler="RecusarIndividual"
                                    name="convidadoId" value="@convidado.Id">Não poderei comparecer</button>
                        }
                    </form>
                }
            }
        </div>
    }
</div>
```

### Grupo

```razor
@{
    var statusGrupo = ConviteInterativo.Web.Pages.Convite.IndexModel.StatusGrupoExibicao(Model.Convidados, Model.Convite, Model.Evento);
    var mostrarBlocoEncGrupo = ConviteInterativo.Web.Pages.Convite.IndexModel.MostrarBlocoPrazoEncerradoGrupo(Model.Convidados, Model.Convite, Model.Evento);
    var refConvidado = Model.Convidados.Count > 0 ? Model.Convidados[0] : null;
    var mostrarConfirmarGrupo = refConvidado is not null && ConviteInterativo.Web.Pages.Convite.IndexModel.MostrarBotaoConfirmar(refConvidado, Model.Convite, Model.Evento);
    var mostrarRecusarGrupo = refConvidado is not null && ConviteInterativo.Web.Pages.Convite.IndexModel.MostrarBotaoRecusar(refConvidado, Model.Convite, Model.Evento);
}

@if (mostrarBlocoEncGrupo)
{
    <p class="pp-prazo-encerrado">Infelizmente o prazo para confirmação se esgotou. Agradecemos pela atenção.</p>
}
else
{
    if (!string.IsNullOrEmpty(statusGrupo))
    {
        <p class="pp-status-grupo">@statusGrupo</p>
    }
    if (mostrarConfirmarGrupo || mostrarRecusarGrupo)
    {
        <form method="post" data-pp-form>
            @if (mostrarConfirmarGrupo)
            {
                <button type="submit" class="btn" asp-page-handler="ConfirmarGrupo">Confirmar presença</button>
            }
            @if (mostrarRecusarGrupo)
            {
                <button type="submit" class="btn btn-secundario" asp-page-handler="RecusarGrupo">Não poderei comparecer</button>
            }
        </form>
    }
}
```

## Partial `_TemaPequenoPrincipe.cshtml`

O bloco de dados do evento (`#pp-dados-evento`) hoje fica no meio do HTML estático (parte do `TemaHtmlMeio`). Como esse HTML é servido inteiro, precisa esconder via CSS quando não deve aparecer.

Adiciona uma classe condicional no container `.pp-pergaminho-conteudo`:

```razor
@{
    var esconderDados = Model.Convite.ModoConfirmacao == ConviteInterativo.Web.Data.Entities.ModoConfirmacao.Individual
        ? Model.Convidados.All(c => !ConviteInterativo.Web.Pages.Convite.IndexModel.MostrarDadosEvento(c, Model.Convite, Model.Evento))
        : !ConviteInterativo.Web.Pages.Convite.IndexModel.MostrarDadosEventoGrupo(Model.Convidados, Model.Convite, Model.Evento);
}

<div class="pp-cena-wrapper @(esconderDados ? "pp-esconder-dados" : "")">
    @* ... resto do conteúdo aqui ... *@
</div>
```

CSS:

```css
.pp-esconder-dados #pp-dados-evento {
    display: none;
}
```

Nota: no Individual, os dados somem só se **nenhum convidado** poderia ver — se ao menos um está Confirmado, todos veem (pra manter consistência com o convite em si). Se todos são Sem resposta/Recusado pós-prazo, esconde.

## CSS novo em `animacao.css`

```css
/* HU-13 — prazo */
.pp-prazo-encerrado {
    font-family: Georgia, 'Times New Roman', serif;
    font-size: 0.85rem;
    font-style: italic;
    color: #4a2f18;
    text-align: center;
    margin: 0.5rem 0;
    line-height: 1.4;
}

.pp-status-inline {
    font-family: Georgia, 'Times New Roman', serif;
    font-size: 0.8rem;
    font-style: italic;
    color: #4a2f18;
    margin: 0 0 0.35rem;
    text-align: center;
}
```

## Guards nos handlers

Cada `OnPost*Async` valida se pode executar antes de chamar service:

```csharp
public async Task<IActionResult> OnPostConfirmarGrupoAsync(string token)
{
    var (dto, notFound) = await CarregarOuNotFoundAsync(token);
    if (dto is null) return notFound!;
    if (dto.Convite.ModoConfirmacao != ModoConfirmacao.Grupo) return BadRequest();

    // HU-13 guard
    var refConvidado = dto.Convidados.Count > 0 ? dto.Convidados[0] : null;
    if (refConvidado is null || !MostrarBotaoConfirmar(refConvidado, dto.Convite, dto.Evento))
    {
        return BadRequest();
    }

    await service.ConfirmarGrupoAsync(dto.Convite.Id, StatusConfirmacao.Confirmado);
    // ... resto igual (notificação, retorno)
}

public async Task<IActionResult> OnPostRecusarGrupoAsync(string token)
{
    var (dto, notFound) = await CarregarOuNotFoundAsync(token);
    if (dto is null) return notFound!;
    if (dto.Convite.ModoConfirmacao != ModoConfirmacao.Grupo) return BadRequest();

    var refConvidado = dto.Convidados.Count > 0 ? dto.Convidados[0] : null;
    if (refConvidado is null || !MostrarBotaoRecusar(refConvidado, dto.Convite, dto.Evento))
    {
        return BadRequest();
    }

    await service.ConfirmarGrupoAsync(dto.Convite.Id, StatusConfirmacao.NaoVai);
    // ... resto igual
}

public async Task<IActionResult> OnPostConfirmarIndividualAsync(string token, int convidadoId)
{
    var (dto, notFound) = await CarregarOuNotFoundAsync(token);
    if (dto is null) return notFound!;
    if (dto.Convite.ModoConfirmacao != ModoConfirmacao.Individual) return BadRequest();

    var convidado = dto.Convidados.FirstOrDefault(c => c.Id == convidadoId);
    if (convidado is null) return NotFound();

    if (!MostrarBotaoConfirmar(convidado, dto.Convite, dto.Evento))
    {
        return BadRequest();
    }

    await service.ConfirmarIndividualAsync(convidadoId, StatusConfirmacao.Confirmado);
    // ... resto igual
}

// Análogo pra OnPostRecusarIndividualAsync com MostrarBotaoRecusar
```

## Cultura pt-BR em produção

`CultureInfo("pt-BR")` funciona em runtime .NET moderno em Linux com ICU instalado. Adicionar no `.csproj`:

```xml
<PropertyGroup>
    <InvariantGlobalization>false</InvariantGlobalization>
</PropertyGroup>
```

## Testes

Novos em `IndexModelTests.cs` (criar arquivo se não existir):

- `EhConviteTardio_criado_antes_de_10_dias_retorna_false`
- `EhConviteTardio_criado_dentro_de_10_dias_retorna_true`
- `PrazoLimite_antecipado_evento_24_10_retorna_14_10_23_59_59`
- `PrazoLimite_tardio_evento_24_10_retorna_22_10_23_59_59`
- `PrazoEncerrado_agora_antes_do_limite_retorna_false`
- `PrazoEncerrado_agora_depois_do_limite_retorna_true`
- `StatusExibicaoIndividual_sem_resposta_pre_prazo_mostra_data`
- `StatusExibicaoIndividual_confirmado_sempre_confirmada`
- `MostrarBotaoConfirmar_confirmado_pre_prazo_false`
- `MostrarBotaoConfirmar_confirmado_pos_prazo_false`
- `MostrarBotaoRecusar_confirmado_pos_prazo_true` (permite imprevisto)
- `MostrarBotaoRecusar_sem_resposta_pos_prazo_false`
- `MostrarDadosEvento_recusado_pos_prazo_false`
- `MostrarDadosEvento_confirmado_pos_prazo_true`

Manter 30 testes existentes verdes.

## Critérios de aceite

Manuais:

**Antes do prazo:**
- Sem resposta: mostra "Confirme se poderá comparecer até 14 de outubro" + 2 botões
- Confirmado: mostra "✓ Presença confirmada" + só botão "Não poderei comparecer"
- Recusado: mostra "✗ Marcado como ausente" + só botão "Confirmar presença"

**Depois do prazo:**
- Confirmado (A): dados visíveis, status "✓ Presença confirmada", só botão "Não poderei comparecer"
- Sem resposta (B): dados somem, mensagem "Infelizmente o prazo para confirmação se esgotou..."
- Recusado (B): idem

**Individual misto (2 confirmados + 3 sem resposta), pós-prazo:**
- Dados do evento visíveis (pelo menos um confirmou)
- Cada bloco de convidado renderiza sua própria mensagem/botões

**Convite tardio criado 8 dias antes do evento:**
- Prazo = antevéspera do evento (2 dias antes) 23:59:59
- Frase "Confirme se poderá comparecer até 22 de outubro"

**POST direto via curl bypass do frontend:**
- Server rejeita com 400 quando ação não é permitida

Automáticos:

- Build 0/0
- 30 atuais + testes novos verdes

## Riscos e mitigações

| Risco | Mitigação |
|---|---|
| `CultureInfo("pt-BR")` falha em Linux produção sem ICU | `InvariantGlobalization=false` no `.csproj` |
| Timezone UTC vs hora local do evento | `Evento.DataHora.Date` abstrai a hora, prazo baseado só em data |
| Convite criado exatamente no 10º dia — antecipado ou tardio? | Regra explícita: `DataCriacao.Date <= (evento.DataHora.Date - 10 dias)` = antecipado. Igual conta como antecipado |
| Convite tardio criado depois da antevéspera do evento — prazo negativo | `PrazoLimite` retorna data no passado, `PrazoEncerrado` retorna true imediatamente. Admin não deveria conseguir criar convite tardio nesse ponto — adicionar validação futura |
| Notificação email dispara antes do guard bloquear | Guard fica antes de `service.Confirmar*` — se guard retorna BadRequest, service não roda, email não dispara |
| HU-12 partial refresh quebra com blocos condicionais | Partial retorna a nova HTML condicional — cada refresh atualiza corretamente. Zero mudança JS |
| Status "vazio" quando pós-prazo — Grupo mostra `<p class="pp-status-grupo">` vazio | `if (!string.IsNullOrEmpty(statusGrupo))` no Razor evita renderizar tag vazia |

## Próxima HU

- **HU-07** — Deploy (Docker + hosting + env vars + HTTPS + ForwardedHeaders). Sessão dedicada, ~3-5h. Considerar habilitar Docker Desktop WSL integration antes.
