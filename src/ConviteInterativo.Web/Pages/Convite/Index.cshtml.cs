using System.Globalization;
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

    private static readonly CultureInfo CulturaPtBr = new("pt-BR");

    private const int DiasGatilhoTardio = 7;
    private const int DiasAntesEventoTardioLongo = 5;
    private const int DiasAntesEventoTardioCurto = 1;

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
            ? $"{convidado.Nome}"
            : $"{convidado.Nome} {convidado.Sobrenome}";

    public static string? FraseConvite(Evento evento) =>
        string.IsNullOrWhiteSpace(evento.Anfitrioes)
            ? null
            : string.IsNullOrWhiteSpace(evento.Homenageado)
                ? $"{evento.Anfitrioes} convidam"
                : $"{evento.Anfitrioes} convidam para\n{evento.Nome}";

    // prazoNormalData = DataHora - evento.DiasConfirmacao (o prazo "de vitrine", configurado no evento).
    // gatilhoTardio = prazoNormalData - 7 dias: convite criado a partir daqui já não tem os
    // DiasConfirmacao inteiros de folga, então cai no regime tardio (HU-15).
    private static (DateTime prazoNormalData, DateTime gatilhoTardio) CalcularLimiares(Evento evento)
    {
        var prazoNormalData = evento.DataHora.Date.AddDays(-evento.DiasConfirmacao);
        return (prazoNormalData, prazoNormalData.AddDays(-DiasGatilhoTardio));
    }

    public static bool EhConviteTardio(Data.Entities.Convite convite, Evento evento)
    {
        var (_, gatilhoTardio) = CalcularLimiares(evento);
        return convite.DataCriacao.Date >= gatilhoTardio;
    }

    public static DateTime PrazoLimite(Data.Entities.Convite convite, Evento evento)
    {
        var (prazoNormalData, gatilhoTardio) = CalcularLimiares(evento);
        var criacao = convite.DataCriacao.Date;

        DateTime limiteData;
        if (criacao < gatilhoTardio)
        {
            limiteData = prazoNormalData;
        }
        else
        {
            var diasAteEvento = (evento.DataHora.Date - criacao).Days;
            limiteData = diasAteEvento >= DiasGatilhoTardio
                ? evento.DataHora.Date.AddDays(-DiasAntesEventoTardioLongo)
                : evento.DataHora.Date.AddDays(-DiasAntesEventoTardioCurto);
        }

        // AddDays(1).AddSeconds(-1) empurra pra 23:59:59 do dia calculado
        return limiteData.AddDays(1).AddSeconds(-1);
    }

    public static string PrazoLimiteFormatado(Data.Entities.Convite convite, Evento evento) =>
        PrazoLimite(convite, evento).ToString("dd 'de' MMMM", CulturaPtBr);

    public static bool PrazoEncerrado(Data.Entities.Convite convite, Evento evento) =>
        DataHoraBrasil.Agora > PrazoLimite(convite, evento);

    public static string StatusExibicaoIndividual(Convidado convidado, Data.Entities.Convite convite, Evento evento)
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

    public static bool MostrarBotaoConfirmar(Convidado convidado, Data.Entities.Convite convite, Evento evento)
    {
        if (PrazoEncerrado(convite, evento))
        {
            return false;
        }

        // Antes do prazo: mostra confirmar se sem resposta ou já recusado (deixa mudar de ideia)
        return convidado.Status != StatusConfirmacao.Confirmado;
    }

    public static bool MostrarBotaoRecusar(Convidado convidado, Data.Entities.Convite convite, Evento evento)
    {
        if (PrazoEncerrado(convite, evento))
        {
            // Pós-prazo: só Confirmado ainda pode cancelar (imprevisto)
            return convidado.Status == StatusConfirmacao.Confirmado;
        }

        // Antes do prazo: mostra recusar se sem resposta ou já confirmado
        return convidado.Status != StatusConfirmacao.NaoVai;
    }

    public static bool MostrarDadosEvento(Convidado convidado, Data.Entities.Convite convite, Evento evento)
    {
        if (!PrazoEncerrado(convite, evento))
        {
            return true;
        }

        return convidado.Status == StatusConfirmacao.Confirmado;
    }

    public static bool MostrarBlocoPrazoEncerrado(Convidado convidado, Data.Entities.Convite convite, Evento evento)
    {
        if (!PrazoEncerrado(convite, evento))
        {
            return false;
        }

        return convidado.Status != StatusConfirmacao.Confirmado;
    }

    // Grupo: usa o primeiro convidado como referência (todos têm mesmo status no Grupo)
    public static string StatusGrupoExibicao(List<Convidado> convidados, Data.Entities.Convite convite, Evento evento) =>
        convidados.Count > 0
            ? StatusExibicaoIndividual(convidados[0], convite, evento)
            : "";

    public static bool MostrarDadosEventoGrupo(List<Convidado> convidados, Data.Entities.Convite convite, Evento evento) =>
        convidados.Count > 0 && MostrarDadosEvento(convidados[0], convite, evento);

    public static bool MostrarBlocoPrazoEncerradoGrupo(List<Convidado> convidados, Data.Entities.Convite convite, Evento evento) =>
        convidados.Count > 0 && MostrarBlocoPrazoEncerrado(convidados[0], convite, evento);

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

        var refConvidado = dto.Convidados.Count > 0 ? dto.Convidados[0] : null;
        if (refConvidado is null || !MostrarBotaoConfirmar(refConvidado, dto.Convite, dto.Evento))
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

        var refConvidado = dto.Convidados.Count > 0 ? dto.Convidados[0] : null;
        if (refConvidado is null || !MostrarBotaoRecusar(refConvidado, dto.Convite, dto.Evento))
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

        var convidado = dto.Convidados.FirstOrDefault(c => c.Id == convidadoId);
        if (convidado is null)
        {
            return NotFound();
        }

        if (!MostrarBotaoConfirmar(convidado, dto.Convite, dto.Evento))
        {
            return BadRequest();
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

        var convidado = dto.Convidados.FirstOrDefault(c => c.Id == convidadoId);
        if (convidado is null)
        {
            return NotFound();
        }

        if (!MostrarBotaoRecusar(convidado, dto.Convite, dto.Evento))
        {
            return BadRequest();
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