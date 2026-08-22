# HU-08 — Anfitriões e Homenageado no Evento

## Objetivo

Adicionar 2 campos opcionais em `Evento` — `Anfitrioes` e `Homenageado` — e exibi-los como frase de convite acima dos nomes dos convidados no pergaminho, tanto no tema Pequeno Príncipe (HU-05b) quanto na view sóbria da HU-05a.

Exemplo de saída: `Rafael e Ana Carolina convidam para o 1º ano do Zayan` — onde "Rafael e Ana Carolina" vem de `Anfitrioes`, e "1º ano do Zayan" vem de `Evento.Nome`.

## Escopo

Dentro:

- 2 colunas nullable em `Evento`: `Anfitrioes` (string, max 200) e `Homenageado` (string, max 100)
- Migration EF adicionando as colunas
- Admin: `EventoInputModel` + `_EventoForm.cshtml` ganham os 2 inputs (opcionais)
- Página pública `/c/{token}`: exibição condicional acima dos nomes dos convidados
- Testes cobrindo persistência + exibição condicional
- Sem regressão nos 25 testes atuais

Fora:

- Enum `TipoEvento` com preposição/frase parametrizada (fica pra HU futura — decisão MVP)
- Redesign do form do admin
- Migration retroativa dos eventos existentes (todos ficam com `NULL` — comportamento atual preservado)

## Regra de exibição

Construída no code-behind ou no template:

- Se `Anfitrioes` **e** `Homenageado` preenchidos:
  `{Anfitrioes} convidam para {Evento.Nome}`
  (ex: "Rafael e Ana Carolina convidam para o 1º ano do Zayan")
- Se só `Anfitrioes` preenchido:
  `{Anfitrioes} convidam`
- Se nem `Anfitrioes` preenchido:
  Não exibe frase — comportamento atual (só nomes dos convidados)

`Homenageado` só entra na frase junto com `Anfitrioes`. Se o admin preencher só `Homenageado` sem `Anfitrioes`, o campo fica ignorado na exibição (mas persiste — pode ser usado depois).

**Decisão MVP** (ADR informal pra registrar aqui): a frase é hardcoded em português com "convidam para {Nome}". Não suporta casamento/chá com sintaxe diferente. Se virar dor, a HU futura adiciona `TipoEvento` enum e frases parametrizadas.

## Modelo

`Data/Entities/Evento.cs`:

```csharp
public string? Anfitrioes { get; set; }
public string? Homenageado { get; set; }
```

`Data/AppDbContext.cs` — Fluent API:

```csharp
builder.Entity<Evento>(e =>
{
    // ... configuração existente
    e.Property(x => x.Anfitrioes).HasMaxLength(200);
    e.Property(x => x.Homenageado).HasMaxLength(100);
});
```

## Migration

Nome sugerido: `AddAnfitrioesHomenageadoToEvento`.

```
dotnet ef migrations add AddAnfitrioesHomenageadoToEvento --project src/ConviteInterativo.Web
```

Migration esperada: `AddColumn<string>` pra `Anfitrioes` e `Homenageado`, ambas `nullable: true`, `maxLength: 200/100`. Down remove ambas.

Aplicar em dev: `dotnet ef database update` — SQLite descartável, sem risco.

## Admin

`Pages/Admin/Eventos/EventoInputModel.cs` (ou onde estiver o input model):

```csharp
[StringLength(200)]
[Display(Name = "Anfitriões")]
public string? Anfitrioes { get; set; }

[StringLength(100)]
[Display(Name = "Homenageado")]
public string? Homenageado { get; set; }
```

Copiar de/pra `Evento` no `Create.cshtml.cs` e `Edit.cshtml.cs` — só mais 2 propriedades no mapping existente.

`Pages/Admin/Eventos/_EventoForm.cshtml` — 2 inputs novos, depois do `Nome`:

```html
<div class="form-group">
    <label asp-for="Input.Anfitrioes"></label>
    <input asp-for="Input.Anfitrioes" class="form-control" />
    <small>Ex: "Rafael e Ana Carolina". Opcional.</small>
    <span asp-validation-for="Input.Anfitrioes" class="text-danger"></span>
</div>

<div class="form-group">
    <label asp-for="Input.Homenageado"></label>
    <input asp-for="Input.Homenageado" class="form-control" />
    <small>Ex: "Zayan". Opcional. Só aparece na frase se Anfitriões também estiver preenchido.</small>
    <span asp-validation-for="Input.Homenageado" class="text-danger"></span>
</div>
```

## Página pública

`Services/ConvitePublicoService.cs`:

O `ConvitePublicoDto` já expõe `Evento` inteiro (a memória confirma isso na HU-05a). Se `Anfitrioes` e `Homenageado` são propriedades da entidade `Evento`, chegam automaticamente via projeção do EF. Nenhuma mudança necessária no service.

`Pages/Convite/Index.cshtml.cs` — helper estático novo:

```csharp
public static string? FraseConvite(Evento evento) =>
    string.IsNullOrWhiteSpace(evento.Anfitrioes)
        ? null
        : string.IsNullOrWhiteSpace(evento.Homenageado)
            ? $"{evento.Anfitrioes} convidam"
            : $"{evento.Anfitrioes} convidam para {evento.Nome}";
```

`Pages/Convite/_TemaPequenoPrincipe.cshtml` — antes do `<div>` dos convidados (Individual) ou antes dos forms (Grupo), dentro do pergaminho:

```razor
@{
    var frase = ConviteInterativo.Web.Pages.Convite.IndexModel.FraseConvite(Model.Evento);
}
@if (frase is not null)
{
    <p class="pp-frase-convite">@frase</p>
}
```

`themes/pequeno-principe/animacao.css` — estilo da frase, na paleta do pergaminho:

```css
.pp-frase-convite {
    font-family: Georgia, 'Times New Roman', serif;
    font-style: italic;
    color: #4a2f18;
    text-align: center;
    margin: 0 0 0.75rem;
    font-size: 0.95rem;
}
```

Também injeta a frase no `window.dadosConvite` do `Index.cshtml` pra caso o JS precisar depois (opcional, pode ficar pra HU futura se não usar agora).

## Testes

Novos em `ConvitePublicoServiceTests.cs`:

- `CarregarPorTokenAsync_retorna_Anfitrioes_e_Homenageado_quando_preenchidos`
- `CarregarPorTokenAsync_retorna_null_em_Anfitrioes_Homenageado_quando_nao_preenchidos`

Novos em `IndexModelTests.cs` (se existir; senão pula):

- `FraseConvite_retorna_null_quando_Anfitrioes_vazio`
- `FraseConvite_retorna_convidam_quando_Homenageado_vazio`
- `FraseConvite_retorna_frase_completa_quando_ambos_preenchidos`

Regressão: 25 testes atuais continuam passando.

## Critérios de aceite

Manuais (Rafael valida):

- Admin: cadastrar evento novo com `Anfitrioes="Rafael e Ana Carolina"` e `Homenageado="Zayan"`, `Nome="1º ano do Zayan"`.
- Página pública `/c/{token}` mostra: "Rafael e Ana Carolina convidam para 1º ano do Zayan" acima dos nomes dos convidados, dentro do pergaminho, no estilo italic/serif definido.
- Cadastrar evento sem `Anfitrioes` → nenhuma frase aparece (comportamento atual).
- Cadastrar evento só com `Anfitrioes` → aparece "Rafael e Ana Carolina convidam".
- Cadastrar evento só com `Homenageado` → nenhuma frase (regra do MVP).
- Editar evento existente → campos permanecem preenchidos no form.

## Riscos e mitigações

| Risco | Mitigação |
|---|---|
| Frase com quebra de linha estranha em mobile (nomes longos + nome do evento longo) | Testar com o caso real ("Rafael e Ana Carolina convidam para 1º ano do Zayan") em viewport mobile antes de fechar |
| Migration falha em banco com dados existentes | Colunas nullable — safe. Testar em dev primeiro |
| Regressão nos 25 testes por mudança em ConvitePublicoDto | Não muda DTO (Evento inteiro já vai) — risco baixo |

## Próximas HUs depois desta

- **HU-06** — Exportação PDF portaria (QuestPDF)
- **HU-07** — Deploy
- **HU-polimento admin** — DateHora com default sensato + navegação (breadcrumb/link Convites)
- **HU futura** — `TipoEvento` enum + frases parametrizadas (se a hardcoded incomodar)
