using ConviteInterativo.Web.Data;
using ConviteInterativo.Web.Data.Entities;
using ConviteInterativo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConviteInterativo.Web.Pages.Admin.Eventos.Convites;

public class EditModel(AppDbContext db, ConviteService conviteService) : PageModel
{
    public Evento Evento { get; set; } = null!;

    [BindProperty]
    public ConviteInputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int eventoId, int id)
    {
        var evento = await db.Eventos.FindAsync(eventoId);
        if (evento is null)
        {
            return NotFound();
        }

        var convite = await db.Convites.FindAsync(id);
        if (convite is null || convite.EventoId != eventoId)
        {
            return NotFound();
        }

        Evento = evento;
        Input = new ConviteInputModel { Nome = convite.Nome, ModoConfirmacao = convite.ModoConfirmacao };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int eventoId, int id)
    {
        var evento = await db.Eventos.FindAsync(eventoId);
        if (evento is null)
        {
            return NotFound();
        }

        Evento = evento;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!await conviteService.NomeDisponivelAsync(eventoId, Input.Nome, id))
        {
            ModelState.AddModelError(nameof(Input.Nome), "Já existe um convite com esse nome neste evento.");
            return Page();
        }

        var convite = await db.Convites.FindAsync(id);
        if (convite is null || convite.EventoId != eventoId)
        {
            return NotFound();
        }

        convite.Nome = Input.Nome;
        convite.ModoConfirmacao = Input.ModoConfirmacao;
        convite.DataAtualizacao = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return RedirectToPage("Details", new { eventoId, id });
    }
}
