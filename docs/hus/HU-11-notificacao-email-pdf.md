# HU-11 — Notificação por email pros anfitriões

## Objetivo

A cada resposta de convidado (Confirmar OU Recusar), enviar email pro endereço dos anfitriões cadastrado no evento. Quando for Confirmação, anexar PDF com a lista atualizada de confirmados (ordem alfabética por nome, formato reutilizável pra portaria no dia da festa).

Absorve o escopo original da HU-06 (exportação PDF portaria) — a mesma geração de PDF que iria na HU-06 vai como anexo aqui.

## Escopo

Dentro:

- Novo campo obrigatório `EmailAnfitrioes` em `Evento` (com migration + form admin)
- Novo `NotificacaoService` com envio SMTP fire-and-forget
- Novo `PdfConfirmadosService` (QuestPDF) — reutilizável no dia da festa também
- Configuração SMTP em `appsettings.json` + credenciais em `appsettings.Development.json` (não commitado)
- Chamada do NotificacaoService nos 4 handlers `OnPost*Async` da página pública, depois do `SaveChangesAsync`
- Assunto: `[{Evento.Nome}] {Convidado.Nome} confirmou presença` / `[{Evento.Nome}] {Convidado.Nome} marcou que não vai`
- Testes: 3-4 novos (envio disparado, PDF anexado só em Confirmação, corpo do email, integração básica com SMTP mockado)

Fora:

- Template Razor HTML pro corpo do email — vai em string simples texto puro
- Reenvio manual (botão "reenviar notificação" no admin)
- Fila persistente com retry — fire-and-forget puro
- Enviar pro convidado também
- Rate limit ou throttle
- Design do PDF sofisticado (cabeçalho estilizado etc) — layout limpo e funcional

## Modelo

`Data/Entities/Evento.cs`:

```csharp
public required string EmailAnfitrioes { get; set; }
```

`Data/AppDbContext.cs` — Fluent API:

```csharp
builder.Entity<Evento>(e =>
{
    // ... configuração existente
    e.Property(x => x.EmailAnfitrioes).HasMaxLength(200).IsRequired();
});
```

## Migration

Nome sugerido: `AddEmailAnfitrioesToEvento`.

**Ponto de atenção**: coluna required (not-null) — eventos já existentes precisam ter valor default temporário. Duas opções:

1. Adiciona nullable primeiro, popula manualmente, altera pra not-null. Custo: 2 migrations.
2. Adiciona com `defaultValue: "rafasgn@hotmail.com"` (ou email genérico) direto not-null. Custo: 1 migration, mas evento existente ganha email chumbado.

Recomendação: **opção 2**. Banco dev descartável, evento existente é seu (`1 ano do zayan`), depois você atualiza pelo admin.

```
dotnet ef migrations add AddEmailAnfitrioesToEvento --project src/ConviteInterativo.Web
```

## Admin

`EventoInputModel.cs`:

```csharp
[Required(ErrorMessage = "Informe o email dos anfitriões.")]
[EmailAddress(ErrorMessage = "Email inválido.")]
[StringLength(200)]
[Display(Name = "Email dos anfitriões")]
public string EmailAnfitrioes { get; set; } = string.Empty;
```

`_EventoForm.cshtml`, depois do `Anfitrioes`:

```html
<label asp-for="EmailAnfitrioes"></label>
<input asp-for="EmailAnfitrioes" type="email" placeholder="Email que receberá as notificações de RSVP" />
<span asp-validation-for="EmailAnfitrioes" class="field-validation-error"></span>
```

`Create.cshtml.cs` e `Edit.cshtml.cs` — mais 1 linha no mapping `Input ↔ Evento`.

## NotificacaoService

Novo em `Services/NotificacaoService.cs`:

```csharp
public interface INotificacaoService
{
    Task NotificarRespostaAsync(Evento evento, Convite convite, Convidado convidado, StatusConfirmacao novoStatus);
}

public class NotificacaoService(
    IOptions<SmtpOptions> smtpOptions,
    PdfConfirmadosService pdfService,
    AppDbContext db,
    ILogger<NotificacaoService> logger) : INotificacaoService
{
    public Task NotificarRespostaAsync(Evento evento, Convite convite, Convidado convidado, StatusConfirmacao novoStatus)
    {
        // fire-and-forget — não bloqueia o PRG do convidado
        _ = Task.Run(async () =>
        {
            try
            {
                var opts = smtpOptions.Value;
                var msg = new MailMessage
                {
                    From = new MailAddress(opts.From, "Convite Interativo"),
                    Subject = ConstruirAssunto(evento, convidado, novoStatus),
                    Body = ConstruirCorpo(evento, convite, convidado, novoStatus),
                    IsBodyHtml = false,
                };
                msg.To.Add(evento.EmailAnfitrioes);

                if (novoStatus == StatusConfirmacao.Confirmado)
                {
                    var pdfBytes = await pdfService.GerarConfirmadosAsync(evento.Id);
                    var nomeArquivo = $"confirmados-{evento.Slug}-{DateTime.UtcNow:yyyyMMdd-HHmm}.pdf";
                    var stream = new MemoryStream(pdfBytes);
                    msg.Attachments.Add(new Attachment(stream, nomeArquivo, "application/pdf"));
                }

                using var client = new SmtpClient(opts.Host, opts.Port)
                {
                    Credentials = new NetworkCredential(opts.Username, opts.Password),
                    EnableSsl = true,
                };
                await client.SendMailAsync(msg);

                logger.LogInformation("Notificação enviada pra {Email} sobre {Nome}", evento.EmailAnfitrioes, convidado.Nome);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao enviar notificação de RSVP");
            }
        });

        return Task.CompletedTask;
    }

    private static string ConstruirAssunto(Evento evento, Convidado convidado, StatusConfirmacao status) =>
        status == StatusConfirmacao.Confirmado
            ? $"[{evento.Nome}] {convidado.Nome} confirmou presença"
            : $"[{evento.Nome}] {convidado.Nome} marcou que não vai";

    private static string ConstruirCorpo(Evento evento, Convite convite, Convidado convidado, StatusConfirmacao status)
    {
        var verbo = status == StatusConfirmacao.Confirmado ? "confirmou presença" : "marcou que não pode comparecer";
        return $$"""
            Olá,

            {{convidado.Nome}} (do grupo "{{convite.Nome}}") {{verbo}} para o evento {{evento.Nome}}.

            Data: {{evento.DataHora:dd/MM/yyyy HH:mm}}
            Local: {{evento.Endereco}}

            {{(status == StatusConfirmacao.Confirmado ? "PDF com a lista atualizada de confirmados em anexo." : "")}}

            --
            Sistema Convite Interativo
            """;
    }
}

public class SmtpOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
}
```

Notas:

- `_ = Task.Run(...)` explicita fire-and-forget e evita warning do compilador
- `logger.LogError` sem re-throw — RSVP não pode falhar por causa de SMTP fora do ar
- SmtpClient descartável dentro do Task pra evitar problema de instância compartilhada

## PdfConfirmadosService

Novo em `Services/PdfConfirmadosService.cs`. Usa QuestPDF (já no projeto).

```csharp
public class PdfConfirmadosService(AppDbContext db)
{
    public async Task<byte[]> GerarConfirmadosAsync(int eventoId)
    {
        var evento = await db.Eventos.FindAsync(eventoId)
            ?? throw new InvalidOperationException("Evento não encontrado.");

        var confirmados = await (
            from c in db.Convidados
            join conv in db.Convites on c.ConviteId equals conv.Id
            where conv.EventoId == eventoId && c.Status == StatusConfirmacao.Confirmado
            orderby c.Nome, c.Sobrenome
            select new { c.Nome, c.Sobrenome, NomeConvite = conv.Nome }
        ).ToListAsync();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text(evento.Nome).FontSize(18).SemiBold();
                    col.Item().Text($"{evento.DataHora:dd/MM/yyyy HH:mm} — {evento.Endereco}").FontSize(10).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(8).Text($"Confirmados: {confirmados.Count}").SemiBold();
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    if (confirmados.Count == 0)
                    {
                        col.Item().Text("Nenhum confirmado ainda.").Italic();
                        return;
                    }

                    foreach (var conf in confirmados)
                    {
                        var display = string.IsNullOrWhiteSpace(conf.Sobrenome)
                            ? $"{conf.Nome} ({conf.NomeConvite})"
                            : $"{conf.Nome} {conf.Sobrenome}";
                        col.Item().Text(display);
                    }
                });

                page.Footer().AlignRight().Text($"Gerado em {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC").FontSize(9).FontColor(Colors.Grey.Medium);
            });
        });

        return doc.GeneratePdf();
    }
}
```

Reutilização: essa mesma classe também vai servir pro botão "Baixar PDF portaria" que pode virar página do admin depois. HU-06 sai do roadmap.

## Configuração SMTP

`appsettings.json` — só a estrutura, sem credenciais:

```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "",
    "Password": "",
    "From": "appconviteinterativo@gmail.com"
  }
}
```

`appsettings.Development.json` — **credenciais reais, adicionar em .gitignore antes de tudo**:

```json
{
  "Smtp": {
    "Username": "appconviteinterativo@gmail.com",
    "Password": "APP_PASSWORD_16_CHARS_DO_GMAIL"
  }
}
```

**Ação manual antes do CC começar**: adiciona em `.gitignore`:

```
appsettings.Development.json
```

E confirma que o arquivo já existente não está trackeado (`git rm --cached appsettings.Development.json` se estiver).

`Program.cs`:

```csharp
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<INotificacaoService, NotificacaoService>();
builder.Services.AddScoped<PdfConfirmadosService>();
```

## Chamada nos handlers públicos

`Pages/Convite/Index.cshtml.cs` — depois do `service.ConfirmarGrupoAsync(...)` e `service.ConfirmarIndividualAsync(...)`, adiciona chamada ao notificacaoService.

Complicação: o método `ConfirmarGrupoAsync` no `ConvitePublicoService` só recebe `conviteId` e `status` — não retorna o `Convite` nem os `Convidados`. Precisa ou:

1. Refatorar o service pra retornar o dto atualizado.
2. Recarregar via `CarregarPorTokenAsync` depois do save (query extra, ~5-10ms).

Recomendação: **opção 2** (recarrega). Simples, sem tocar em assinatura testada.

Nos handlers Grupo, envia 1 email por convidado do grupo (todos foram atualizados no batch):

```csharp
public async Task<IActionResult> OnPostConfirmarGrupoAsync(string token)
{
    var (dto, notFound) = await CarregarOuNotFoundAsync(token);
    if (dto is null) return notFound!;
    if (dto.Convite.ModoConfirmacao != ModoConfirmacao.Grupo) return BadRequest();

    await service.ConfirmarGrupoAsync(dto.Convite.Id, StatusConfirmacao.Confirmado);

    // Recarrega e notifica
    var atualizado = await service.CarregarPorTokenAsync(token);
    if (atualizado is not null)
    {
        foreach (var c in atualizado.Convidados)
        {
            await notificacaoService.NotificarRespostaAsync(
                atualizado.Evento, atualizado.Convite, c, StatusConfirmacao.Confirmado);
        }
    }

    return RedirectToPage(new { token });
}
```

Individual dispara 1 email só (do convidado que clicou).

## Testes

Novos em `ConvitePublicoServiceTests.cs`:

- `CarregarPorTokenAsync_retorna_EmailAnfitrioes_do_Evento`
- (opcional) migration não quebra 27 testes existentes — validado automaticamente por não mudar assinatura

Novo arquivo `NotificacaoServiceTests.cs`:

- `NotificarRespostaAsync_confirmacao_gera_pdf_anexado` — mock do `SmtpClient` não trivial; alternativa é testar `PdfConfirmadosService.GerarConfirmadosAsync` isolado
- `PdfConfirmadosService_gera_PDF_com_confirmados_ordenados` — dado 3 convidados (Bruno, Ana, Carla) todos confirmados, PDF gerado tem os 3 nomes na ordem A→B→C

Meta: testes cobrem geração do PDF e comportamento do service; envio SMTP real fica pra teste manual.

## Config Gmail App Password

Pré-requisitos que só você pode fazer:

1. Loga em `appconviteinterativo@gmail.com`
2. Ativa 2FA em https://myaccount.google.com/security
3. Gera app password em https://myaccount.google.com/apppasswords → nome "Convite Interativo" → copia os 16 caracteres
4. Cola em `appsettings.Development.json` no `Password` (já com o Development.json fora do Git)

## Critérios de aceite

Manuais:

- **Cria evento** com email válido, confirma que salva
- **Confirmação individual** → recebe email com "confirmou presença" no assunto + PDF anexo
- **Recusa individual** → recebe email com "marcou que não vai" no assunto, sem anexo
- **Confirmação Grupo com N convidados** → recebe N emails (um por convidado)
- **SMTP config errada** → PRG do convidado funciona normalmente, email não chega, log de erro registrado
- **PDF anexo** → abre no leitor, contém nome do evento, contagem de confirmados, lista alfabética

Automatizados:

- Build 0/0
- 27 testes atuais continuam passando
- 2-3 testes novos (PDF geração + service basics)

## Riscos e mitigações

| Risco | Mitigação |
|---|---|
| Gmail marca como spam nos primeiros dias | Testar com você mesmo primeiro; adicionar remetente aos contatos; SPF/DKIM já vem do Gmail |
| SMTP fora do ar bloqueia UX do convidado | Fire-and-forget com try/catch — RSVP grava e redirecionar independente |
| Batch de N emails no Grupo demora | Fire-and-forget não bloqueia — enfileira N tasks paralelas |
| App password vaza no Git | .gitignore + git rm --cached antes do primeiro commit |
| DbContext scoped acessado em `Task.Run` fora do scope original | Injetar IServiceScopeFactory e criar scope novo dentro da Task — ajuste no NotificacaoService se der pau em produção |
| Evento existente sem email quebra o form Edit | Migration com defaultValue chumbado + admin atualiza |
| PDF com nome longo estoura layout | QuestPDF quebra linha automaticamente; testar com nome grande |

## Consideração técnica sobre `Task.Run` e DbContext

`AppDbContext` é scoped. Chamar `pdfService.GerarConfirmadosAsync` de dentro do `Task.Run` acessa o mesmo `db` do request original — se o request terminar antes da task, o context vira zumbi.

Duas opções:

1. **Injetar `IServiceScopeFactory`** e criar scope novo dentro da Task:

```csharp
_ = Task.Run(async () =>
{
    using var scope = scopeFactory.CreateScope();
    var localDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var localPdf = scope.ServiceProvider.GetRequiredService<PdfConfirmadosService>();
    // ... usa localDb, localPdf
});
```

2. **Buffer os dados antes do `Task.Run`** — carrega tudo que precisa antes, passa como argumento pro fire-and-forget. Task não acessa DbContext.

Recomendação: **opção 2** (buffer antes) — mais previsível, menos DI dance. Handler consulta os dados que precisa, passa lista/objetos como argumento pro service. Service só monta email + envia.

Ajuste do handler:

```csharp
public async Task<IActionResult> OnPostConfirmarGrupoAsync(string token)
{
    var (dto, notFound) = await CarregarOuNotFoundAsync(token);
    if (dto is null) return notFound!;
    if (dto.Convite.ModoConfirmacao != ModoConfirmacao.Grupo) return BadRequest();

    await service.ConfirmarGrupoAsync(dto.Convite.Id, StatusConfirmacao.Confirmado);

    var atualizado = await service.CarregarPorTokenAsync(token);
    if (atualizado is not null)
    {
        // gera PDF SÍNCRONO aqui, antes do Task.Run
        var pdfBytes = await pdfService.GerarConfirmadosAsync(atualizado.Evento.Id);

        foreach (var c in atualizado.Convidados)
        {
            notificacaoService.NotificarResposta(
                atualizado.Evento, atualizado.Convite, c,
                StatusConfirmacao.Confirmado, pdfBytes);
        }
    }

    return RedirectToPage(new { token });
}
```

`NotificacaoService.NotificarResposta` (não-async agora) recebe `byte[]? pdfBytes` como argumento, só monta MailMessage e dispara SmtpClient dentro do `Task.Run`. Sem DbContext dentro do fire-and-forget.

## Próximas HUs

- **HU-07** — Deploy (crítico, 29 dias). Precisa: hosting Linux (Railway ou Fly.io), ForwardedHeaders, USER non-root, config SMTP em env vars (não appsettings.Development.json que fica só local)
- **HU polimento** — toast pós-confirmação, breadcrumb, débito :hover botões
