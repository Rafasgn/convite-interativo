using ConviteInterativo.Web.Data.Entities;
using ConviteInterativo.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConviteInterativo.Web.Pages.Convite;

public class IndexModel(ConvitePublicoService service, IWebHostEnvironment env) : PageModel
{
    private const string MarcadorInicio = "<!--CONFIRMACAO_INICIO-->";
    private const string MarcadorFim = "<!--CONFIRMACAO_FIM-->";

    public Evento Evento { get; set; } = null!;
    public Data.Entities.Convite Convite { get; set; } = null!;
    public List<Convidado> Convidados { get; set; } = [];

    public string TemaHtmlAntes { get; private set; } = string.Empty;
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

        return RedirectToPage(new { token });
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

        return RedirectToPage(new { token });
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

        return RedirectToPage(new { token });
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

        return RedirectToPage(new { token });
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
        var html = await File.ReadAllTextAsync(caminho);

        var indiceInicio = html.IndexOf(MarcadorInicio, StringComparison.Ordinal);
        var indiceFim = html.IndexOf(MarcadorFim, StringComparison.Ordinal);

        if (indiceInicio < 0 || indiceFim < 0 || indiceFim < indiceInicio)
        {
            throw new InvalidOperationException("Marcadores de confirmação não encontrados em animacao.html.");
        }

        TemaHtmlAntes = html[..(indiceInicio + MarcadorInicio.Length)];
        TemaHtmlDepois = html[indiceFim..];
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
}
