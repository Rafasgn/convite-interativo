using ConviteInterativo.Web.Data;
using ConviteInterativo.Web.Data.Entities;
using ConviteInterativo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConviteInterativo.Web.Pages.Admin.Eventos.Convites;

public class DetailsModel(AppDbContext db, ConvidadoService convidadoService) : PageModel
{
    public Evento Evento { get; set; } = null!;
    public Convite Convite { get; set; } = null!;
    public List<Convidado> Convidados { get; set; } = [];

    [BindProperty]
    public ConvidadoInputModel NovoConvidado { get; set; } = new();

    [TempData]
    public string? ErroConvidado { get; set; }

    public string LinkPublico => $"{Request.Scheme}://{Request.Host}/c/{Convite.Token}";

    public async Task<IActionResult> OnGetAsync(int eventoId, int id)
    {
        var (ok, notFound) = await GuardaEventoConviteAsync(eventoId, id);
        if (!ok)
        {
            return notFound!;
        }

        Convidados = await convidadoService.ListarAsync(id);

        return Page();
    }

    public async Task<IActionResult> OnPostAdicionarAsync(int eventoId, int id)
    {
        var (ok, notFound) = await GuardaEventoConviteAsync(eventoId, id);
        if (!ok)
        {
            return notFound!;
        }

        if (string.IsNullOrWhiteSpace(NovoConvidado.Nome))
        {
            ErroConvidado = "Informe o nome do integrante.";
            return RedirectToPage(new { eventoId, id });
        }

        await convidadoService.AdicionarAsync(id, NovoConvidado.Nome, NovoConvidado.Sobrenome);

        return RedirectToPage(new { eventoId, id });
    }

    public async Task<IActionResult> OnPostEditarAsync(int eventoId, int id, int convidadoId, string nome, string? sobrenome)
    {
        var (ok, notFound) = await GuardaEventoConviteAsync(eventoId, id);
        if (!ok)
        {
            return notFound!;
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            ErroConvidado = "Informe o nome do integrante.";
            return RedirectToPage(new { eventoId, id });
        }

        await convidadoService.EditarAsync(id, convidadoId, nome, sobrenome);

        return RedirectToPage(new { eventoId, id });
    }

    public async Task<IActionResult> OnPostRemoverAsync(int eventoId, int id, int convidadoId)
    {
        var (ok, notFound) = await GuardaEventoConviteAsync(eventoId, id);
        if (!ok)
        {
            return notFound!;
        }

        await convidadoService.RemoverAsync(id, convidadoId);

        return RedirectToPage(new { eventoId, id });
    }

    private async Task<(bool ok, IActionResult? notFound)> GuardaEventoConviteAsync(int eventoId, int id)
    {
        var evento = await db.Eventos.FindAsync(eventoId);
        if (evento is null)
        {
            return (false, NotFound());
        }

        var convite = await db.Convites.FindAsync(id);
        if (convite is null || convite.EventoId != eventoId)
        {
            return (false, NotFound());
        }

        Evento = evento;
        Convite = convite;

        return (true, null);
    }
}
