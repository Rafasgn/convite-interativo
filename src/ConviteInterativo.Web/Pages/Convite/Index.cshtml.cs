using ConviteInterativo.Web.Data.Entities;
using ConviteInterativo.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConviteInterativo.Web.Pages.Convite;

public class IndexModel(
    ConvitePublicoService service,
    IWebHostEnvironment env,
    PdfConfirmadosService pdfService,
    INotificacaoService notificacaoService) : PageModel
{
    private const string MarcadorFraseInicio = "<!--FRASE_CONVITE_INICIO-->";
    private const string MarcadorFraseFim = "<!--FRASE_CONVITE_FIM-->";
    private const string MarcadorConfirmacaoInicio = "<!--CONFIRMACAO_INICIO-->";
    private const string MarcadorConfirmacaoFim = "<!--CONFIRMACAO_FIM-->";

    public Evento Evento { get; set; } = null!;
    public Data.Entities.Convite Convite { get; set; } = null!;
    public List<Convidado> Convidados { get; set; } = [];

    public string TemaHtmlAntes { get; private set; } = string.Empty;
    public string TemaHtmlMeio { get; private set; } = string.Empty;
    public string TemaHtmlDepois { get; private set; } = string.Empty;

    public string LinkMapaResolvido => Evento.LinkMapa is { Length: > 0 }
        ? Evento.LinkMapa
        : $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(Evento.Endereco)}";

    public static string NomeExibicao(Convidado convidado, string nomeConvite) =>
        string.IsNullOrWhiteSpace(convidado.Sobrenome)
            ? $"{convidado.Nome} ({nomeConvite})"
            : $"{convidado.Nome} {convidado.Sobrenome}";

    public static string StatusExibicao(StatusConfirmacao status) => status switch
    {
        StatusConfirmacao.Confirmado => "Confirmado",
        StatusConfirmacao.NaoVai => "Não vai",
        _ => "Sem resposta",
    };

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
            _ => null,
        };
    }

    public static string? FraseConvite(Evento evento) =>
        string.IsNullOrWhiteSpace(evento.Anfitrioes)
            ? null
            : string.IsNullOrWhiteSpace(evento.Homenageado)
                ? $"{evento.Anfitrioes} convidam"
                : $"{evento.Anfitrioes} convidam para\n{evento.Nome}";

    public async Task<IActionResult> OnGetAsync(string token)
    {
        var (dto, notFound) = await CarregarOuNotFoundAsync(token);
        if (dto is null)
        {
            return notFound!;
        }

        CarregarDto(dto);
        await CarregarTemaHtmlAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmarGrupoAsync(string token)
    {
        var (dto, notFound) = await CarregarOuNotFoundAsync(token);
        if (dto is null)
        {
            return notFound!;
        }

        if (dto.Convite.ModoConfirmacao != ModoConfirmacao.Grupo)
        {
            return BadRequest();
        }

        await service.ConfirmarGrupoAsync(dto.Convite.Id, StatusConfirmacao.Confirmado);

        var atualizado = await service.CarregarPorTokenAsync(token);
        if (atualizado is not null)
        {
            var pdfBytes = await pdfService.GerarConfirmadosAsync(atualizado.Evento.Id);

            foreach (var convidadoAtualizado in atualizado.Convidados)
            {
                notificacaoService.NotificarResposta(
                    atualizado.Evento, atualizado.Convite, convidadoAtualizado,
                    StatusConfirmacao.Confirmado, pdfBytes);
            }
        }

        return await RetornarPartialOuRedirectAsync(token);
    }

    public async Task<IActionResult> OnPostRecusarGrupoAsync(string token)
    {
        var (dto, notFound) = await CarregarOuNotFoundAsync(token);
        if (dto is null)
        {
            return notFound!;
        }

        if (dto.Convite.ModoConfirmacao != ModoConfirmacao.Grupo)
        {
            return BadRequest();
        }

        await service.ConfirmarGrupoAsync(dto.Convite.Id, StatusConfirmacao.NaoVai);

        var atualizado = await service.CarregarPorTokenAsync(token);
        if (atualizado is not null)
        {
            foreach (var convidadoAtualizado in atualizado.Convidados)
            {
                notificacaoService.NotificarResposta(
                    atualizado.Evento, atualizado.Convite, convidadoAtualizado,
                    StatusConfirmacao.NaoVai, null);
            }
        }

        return await RetornarPartialOuRedirectAsync(token);
    }

    public async Task<IActionResult> OnPostConfirmarIndividualAsync(string token, int convidadoId)
    {
        var (dto, notFound) = await CarregarOuNotFoundAsync(token);
        if (dto is null)
        {
            return notFound!;
        }

        if (dto.Convite.ModoConfirmacao != ModoConfirmacao.Individual)
        {
            return BadRequest();
        }

        if (!dto.Convidados.Any(c => c.Id == convidadoId))
        {
            return NotFound();
        }

        await service.ConfirmarIndividualAsync(convidadoId, StatusConfirmacao.Confirmado);

        var atualizado = await service.CarregarPorTokenAsync(token);
        var convidadoAtualizado = atualizado?.Convidados.FirstOrDefault(c => c.Id == convidadoId);
        if (atualizado is not null && convidadoAtualizado is not null)
        {
            var pdfBytes = await pdfService.GerarConfirmadosAsync(atualizado.Evento.Id);
            notificacaoService.NotificarResposta(
                atualizado.Evento, atualizado.Convite, convidadoAtualizado,
                StatusConfirmacao.Confirmado, pdfBytes);
        }

        return await RetornarPartialOuRedirectAsync(token);
    }

    public async Task<IActionResult> OnPostRecusarIndividualAsync(string token, int convidadoId)
    {
        var (dto, notFound) = await CarregarOuNotFoundAsync(token);
        if (dto is null)
        {
            return notFound!;
        }

        if (dto.Convite.ModoConfirmacao != ModoConfirmacao.Individual)
        {
            return BadRequest();
        }

        if (!dto.Convidados.Any(c => c.Id == convidadoId))
        {
            return NotFound();
        }

        await service.ConfirmarIndividualAsync(convidadoId, StatusConfirmacao.NaoVai);

        var atualizado = await service.CarregarPorTokenAsync(token);
        var convidadoAtualizado = atualizado?.Convidados.FirstOrDefault(c => c.Id == convidadoId);
        if (atualizado is not null && convidadoAtualizado is not null)
        {
            notificacaoService.NotificarResposta(
                atualizado.Evento, atualizado.Convite, convidadoAtualizado,
                StatusConfirmacao.NaoVai, null);
        }

        return await RetornarPartialOuRedirectAsync(token);
    }

    private void CarregarDto(ConvitePublicoDto dto)
    {
        Evento = dto.Evento;
        Convite = dto.Convite;
        Convidados = dto.Convidados;
    }

    private async Task CarregarTemaHtmlAsync()
    {
        var caminho = Path.Combine(env.ContentRootPath, "..", "..", "themes", "pequeno-principe", "animacao.html");
        var html = await System.IO.File.ReadAllTextAsync(caminho);

        var indiceFraseInicio = html.IndexOf(MarcadorFraseInicio, StringComparison.Ordinal);
        var indiceFraseFim = html.IndexOf(MarcadorFraseFim, StringComparison.Ordinal);
        var indiceConfirmacaoInicio = html.IndexOf(MarcadorConfirmacaoInicio, StringComparison.Ordinal);
        var indiceConfirmacaoFim = html.IndexOf(MarcadorConfirmacaoFim, StringComparison.Ordinal);

        if (indiceFraseInicio < 0 || indiceFraseFim < 0 || indiceFraseFim < indiceFraseInicio ||
            indiceConfirmacaoInicio < 0 || indiceConfirmacaoFim < 0 || indiceConfirmacaoFim < indiceConfirmacaoInicio ||
            indiceConfirmacaoInicio < indiceFraseFim)
        {
            throw new InvalidOperationException("Marcadores de frase/confirmação não encontrados (ou fora de ordem) em animacao.html.");
        }

        TemaHtmlAntes = html[..(indiceFraseInicio + MarcadorFraseInicio.Length)];
        TemaHtmlMeio = html[indiceFraseFim..(indiceConfirmacaoInicio + MarcadorConfirmacaoInicio.Length)];
        TemaHtmlDepois = html[indiceConfirmacaoFim..];
    }

    private async Task<(ConvitePublicoDto? dto, IActionResult? notFound)> CarregarOuNotFoundAsync(string token)
    {
        var dto = await service.CarregarPorTokenAsync(token);
        if (dto is null)
        {
            return (null, NotFound());
        }

        return (dto, null);
    }

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
}